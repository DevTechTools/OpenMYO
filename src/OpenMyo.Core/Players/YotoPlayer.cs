namespace OpenMyo.Core.Players;

/// <summary>A Yoto player on the user's account that OpenMyo can send playback to.</summary>
public sealed record YotoPlayer(string DeviceId, string Name, bool IsOnline);
