using OpenMyo.Core.Players;

namespace OpenMyo.Core.Abstractions;

/// <summary>
/// Remote control for the user's Yoto players. Each player has one connection, shared by everything that talks
/// to it; a connected player reports its state through <see cref="StateChanged"/>.
/// </summary>
public interface IPlayerRemote
{
    /// <summary>
    /// Raised, on a background thread, when a connected player reports state, or when it stops answering
    /// (<see cref="PlayerStateChangedEventArgs.IsConnected"/> false, after one reconnect attempt).
    /// </summary>
    event EventHandler<PlayerStateChangedEventArgs>? StateChanged;

    /// <summary>The last state a connected player reported, or null when it is not connected.</summary>
    PlayerState? LastState(string deviceId);

    /// <summary>Connects to the player and starts listening to it; does nothing when already connected.</summary>
    Task ConnectAsync(string deviceId, CancellationToken cancellationToken);

    /// <summary>Stops listening; playback on the player carries on.</summary>
    Task DisconnectAsync(string deviceId, CancellationToken cancellationToken);

    Task DisconnectAllAsync(CancellationToken cancellationToken);

    /// <summary>Starts a card on the player; connects first when needed.</summary>
    Task PlayAsync(string deviceId, PlayTarget target, CancellationToken cancellationToken);

    Task SendAsync(string deviceId, PlayerCommand command, CancellationToken cancellationToken);

    /// <summary>Sets the player's volume in its own steps, 0–16; the player keeps to its parental limit.</summary>
    Task SetVolumeAsync(string deviceId, int level, CancellationToken cancellationToken);
}
