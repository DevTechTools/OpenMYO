using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;

namespace OpenMyo.Mcp.Tests.Fakes;

/// <summary>Records what was sent to which player, as "connect d1", "d1 play card#1@0", "d1 pause", "d1 volume 8", "disconnect d1".</summary>
public sealed class RecordingPlayerRemote : IPlayerRemote
{
    private readonly HashSet<string> _connected = new(StringComparer.Ordinal);

    public event EventHandler<PlayerStateChangedEventArgs>? StateChanged
    {
        add { }
        remove { }
    }

    public List<string> Log { get; } = [];

    public PlayerState? LastState(string deviceId) => _connected.Contains(deviceId) ? PlayerState.Empty : null;

    public Task ConnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        _connected.Add(deviceId);
        Log.Add($"connect {deviceId}");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        _connected.Remove(deviceId);
        Log.Add($"disconnect {deviceId}");
        return Task.CompletedTask;
    }

    public async Task DisconnectAllAsync(CancellationToken cancellationToken)
    {
        foreach (var deviceId in _connected.ToList())
        {
            await DisconnectAsync(deviceId, cancellationToken);
        }
    }

    public Task PlayAsync(string deviceId, PlayTarget target, CancellationToken cancellationToken)
    {
        Log.Add($"{deviceId} play {target.CardId}#{target.ChapterKey}@{target.StartAt.TotalSeconds}");
        return Task.CompletedTask;
    }

    public Task SendAsync(string deviceId, PlayerCommand command, CancellationToken cancellationToken)
    {
        Log.Add($"{deviceId} {command.ToString().ToLowerInvariant()}");
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(string deviceId, int level, CancellationToken cancellationToken)
    {
        Log.Add($"{deviceId} volume {level}");
        return Task.CompletedTask;
    }
}
