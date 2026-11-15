using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;
using OpenMyo.Yoto.Auth;

namespace OpenMyo.Yoto.Devices;

/// <summary>
/// Controls Yoto players over Yoto's MQTT broker, with one shared <see cref="PlayerConnection"/> per player.
/// The broker authorises each connection with the user's access token (scope <c>family:devices:control</c>).
/// </summary>
internal sealed class YotoPlayerRemote(IAccessTokenSource tokens, ILogger<YotoPlayerRemote> logger)
    : IPlayerRemote, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, PlayerConnection> _connections = new(StringComparer.Ordinal);

    public event EventHandler<PlayerStateChangedEventArgs>? StateChanged;

    public PlayerState? LastState(string deviceId) =>
        _connections.TryGetValue(deviceId, out var connection) ? connection.State : null;

    public Task ConnectAsync(string deviceId, CancellationToken cancellationToken) =>
        Connection(deviceId).OpenAsync(cancellationToken);

    public async Task DisconnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        if (_connections.TryRemove(deviceId, out var connection))
        {
            await connection.DisposeAsync();
        }
    }

    public async Task DisconnectAllAsync(CancellationToken cancellationToken)
    {
        foreach (var deviceId in _connections.Keys.ToList())
        {
            await DisconnectAsync(deviceId, cancellationToken);
        }
    }

    public Task PlayAsync(string deviceId, PlayTarget target, CancellationToken cancellationToken) =>
        Connection(deviceId).PublishAsync("card/start", PlayerMessages.StartPayload(target), cancellationToken);

    public Task SendAsync(string deviceId, PlayerCommand command, CancellationToken cancellationToken) =>
        Connection(deviceId).PublishAsync(PlayerMessages.CommandName(command), string.Empty, cancellationToken);

    public Task SetVolumeAsync(string deviceId, int level, CancellationToken cancellationToken) =>
        Connection(deviceId).PublishAsync("volume/set", PlayerMessages.VolumePayload(level), cancellationToken);

    public ValueTask DisposeAsync() => new(DisconnectAllAsync(CancellationToken.None));

    private PlayerConnection Connection(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return _connections.GetOrAdd(deviceId, id => new PlayerConnection(id, tokens, logger, Report));
    }

    private void Report(PlayerStateChangedEventArgs change) => StateChanged?.Invoke(this, change);
}
