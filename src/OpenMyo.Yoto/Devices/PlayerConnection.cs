using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using OpenMyo.Core.Players;
using OpenMyo.Yoto.Auth;

namespace OpenMyo.Yoto.Devices;

/// <summary>
/// The one MQTT connection to one Yoto player (AWS IoT over secure WebSockets). The broker allows a single
/// connection per player, so everything in the app that talks to a player shares this one. It keeps the
/// player's merged state, keeps the connection open, and reconnects once when it drops.
/// </summary>
internal sealed partial class PlayerConnection(
    string deviceId, IAccessTokenSource tokens, ILogger logger, Action<PlayerStateChangedEventArgs> report)
    : IAsyncDisposable
{
    private const string BrokerUri = "wss://aqrphjqbp3u2z-ats.iot.eu-west-2.amazonaws.com/mqtt";
    private const string AuthorizerSuffix = "?x-amz-customauthorizer-name=PublicJWTAuthorizer";

    // Yoto closes connections that are quiet for about five minutes; asking for events just before keeps it open.
    private static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(295);

    // Connections drop for ordinary reasons (Wi-Fi blips, an expired token, or Claude Desktop briefly taking the
    // player's single connection), so wait a moment and try once more before reporting the player as gone.
    private static readonly TimeSpan RecoverDelay = TimeSpan.FromSeconds(3);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IMqttClient? _client;
    private CancellationTokenSource? _keepAlive;
    private PlayerState _state = PlayerState.Empty;
    private bool _isClosed;

    public string DeviceId { get; } = deviceId;

    public PlayerState State => _state;

    /// <summary>Connects and starts listening, unless already connected.</summary>
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _isClosed = false;
            await EnsureOpenAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Sends a command, reconnecting first (with a fresh token) when the connection has dropped.</summary>
    public async Task PublishAsync(string command, string payload, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _isClosed = false;
            var client = await EnsureOpenAsync(cancellationToken);
            await client.PublishStringAsync(PlayerMessages.Command(DeviceId, command), payload, cancellationToken: cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _isClosed = true;
            await DropClientAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(CancellationToken.None);
        _gate.Dispose();
    }

    private async Task<IMqttClient> EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (_client is { IsConnected: true } open)
        {
            return open;
        }

        await DropClientAsync(cancellationToken);
        var client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += args =>
        {
            OnMessage(args.ApplicationMessage.ConvertPayloadToString());
            return Task.CompletedTask;
        };
        client.DisconnectedAsync += args =>
        {
            if (args.ClientWasConnected && ReferenceEquals(client, _client) && !_isClosed)
            {
                LogDisconnected(logger, args.Reason);
                _ = RecoverAsync();
            }

            return Task.CompletedTask;
        };

        _client = client;
        await client.ConnectAsync(await BuildOptionsAsync(cancellationToken), cancellationToken);
        await client.SubscribeAsync(PlayerMessages.Events(DeviceId), cancellationToken: cancellationToken);
        await client.PublishStringAsync(PlayerMessages.EventsRequest(DeviceId), "{}", cancellationToken: cancellationToken);
        StartKeepAlive(client);
        return client;
    }

    private async Task<MqttClientOptions> BuildOptionsAsync(CancellationToken cancellationToken)
    {
        var token = await tokens.GetAccessTokenAsync(cancellationToken);
        return new MqttClientOptionsBuilder()
            .WithWebSocketServer(socket => socket.WithUri(BrokerUri))
            .WithTlsOptions(tls => tls.UseTls())
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithClientId("DASH" + DeviceId)
            .WithCredentials(DeviceId + AuthorizerSuffix, token)
            .WithKeepAlivePeriod(KeepAlive)
            .WithCleanSession()
            // AWS IoT drops the connection when a packet is split across WebSocket frames, which MQTTnet does by
            // default for anything with a payload; subscribing still works, so the failure only shows on commands.
            .WithoutPacketFragmentation()
            .Build();
    }

    /// <summary>Never throws: reports the player as disconnected when the second attempt fails too.</summary>
    private async Task RecoverAsync()
    {
        await Task.Delay(RecoverDelay);
        try
        {
            if (!_isClosed)
            {
                await OpenAsync(CancellationToken.None);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Background work with no caller to throw to: the failure is reported as an event instead.
            LogDropped(logger, ex.Message);
            report(new PlayerStateChangedEventArgs(DeviceId, _state, isConnected: false));
        }
    }

    private async Task DropClientAsync(CancellationToken cancellationToken)
    {
        _keepAlive?.Cancel();
        _keepAlive?.Dispose();
        _keepAlive = null;
        if (_client is not { } client)
        {
            return;
        }

        _client = null;
        if (client.IsConnected)
        {
            await client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
        }

        client.Dispose();
    }

    private void StartKeepAlive(IMqttClient client)
    {
        var stop = new CancellationTokenSource();
        _keepAlive = stop;
        _ = KeepAliveAsync(client, stop.Token);
    }

    /// <summary>Never throws: it ends quietly when stopped or when the connection has gone.</summary>
    private async Task KeepAliveAsync(IMqttClient client, CancellationToken stop)
    {
        using var timer = new PeriodicTimer(KeepAlive);
        try
        {
            while (await timer.WaitForNextTickAsync(stop) && client.IsConnected)
            {
                await client.PublishStringAsync(PlayerMessages.EventsRequest(DeviceId), "{}", cancellationToken: stop);
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped because the connection was closed or replaced.
        }
        catch (MQTTnet.Exceptions.MqttCommunicationException ex)
        {
            LogDropped(logger, ex.Message);
        }
    }

    private void OnMessage(string json)
    {
        try
        {
            if (PlayerMessages.ParseEvents(json) is { } update)
            {
                _state = _state.Merge(update);
                report(new PlayerStateChangedEventArgs(DeviceId, _state, isConnected: true));
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            LogBadMessage(logger, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Yoto player connection closed: {Reason}")]
    private static partial void LogDropped(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Yoto player disconnected: {Reason}")]
    private static partial void LogDisconnected(ILogger logger, MqttClientDisconnectReason reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored an unreadable Yoto player message: {Error}")]
    private static partial void LogBadMessage(ILogger logger, string error);
}
