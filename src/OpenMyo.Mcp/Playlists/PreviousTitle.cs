namespace OpenMyo.Mcp.Playlists;

/// <summary>A track's title before a rename; pass it back to rename_tracks to undo.</summary>
public sealed record PreviousTitle(int Track, string Title);
