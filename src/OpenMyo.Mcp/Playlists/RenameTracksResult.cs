namespace OpenMyo.Mcp.Playlists;

/// <summary>rename_tracks result. <paramref name="Previous"/> lets Claude undo the change.</summary>
public sealed record RenameTracksResult(int Saved, IReadOnlyList<PreviousTitle> Previous);
