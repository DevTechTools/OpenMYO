namespace OpenMyo.Core.Players;

/// <summary>A player reported new state, or stopped being reachable (<see cref="IsConnected"/> false).</summary>
public sealed class PlayerStateChangedEventArgs(string deviceId, PlayerState state, bool isConnected) : EventArgs
{
    public string DeviceId { get; } = deviceId;

    public PlayerState State { get; } = state;

    public bool IsConnected { get; } = isConnected;
}
