namespace OpenMyo.Core.Import;

/// <summary>A playlist built from local files that has not been uploaded yet.</summary>
/// <param name="SourceLabel">Where the files came from, shown instead of <paramref name="SourceFolder"/> when that is a temporary download folder.</param>
public sealed record PlaylistDraft(
    string Title,
    string SourceFolder,
    IReadOnlyList<TrackDraft> Tracks,
    ArtworkSource? Cover,
    string? SourceLabel = null);
