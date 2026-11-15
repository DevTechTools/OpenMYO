using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Icons;

/// <summary>A playlist with icons filled in, and how many of them came from yotoicons.com.</summary>
public sealed record IconMatchResult(Playlist Playlist, int CommunityIconCount);
