using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Icons;

/// <summary>The renumbered playlist, plus the track numbers Yoto has no number icon for.</summary>
public sealed record NumberingResult(Playlist Playlist, IReadOnlyList<int> MissingNumbers);
