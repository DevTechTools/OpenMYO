namespace OpenMyo.Mcp.Playlists;

/// <summary>rename_playlist result. Pass <paramref name="Previous"/> back to rename_playlist to undo.</summary>
public sealed record RenamePlaylistResult(string Title, string Previous);
