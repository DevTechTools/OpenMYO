namespace OpenMyo.Core.Import;

/// <summary>A local audio file prepared for upload, with its cleaned title and chosen icon.</summary>
/// <param name="IconArtwork">A picture found next to or inside the file, uploaded as a custom icon when no icon is chosen.</param>
/// <param name="IconMediaId">An existing Yoto icon chosen by matching or by the user. Wins over <paramref name="IconArtwork"/>.</param>
public sealed record TrackDraft(
    string SourcePath,
    string Title,
    TimeSpan Duration,
    ArtworkSource? IconArtwork = null,
    string? IconMediaId = null);
