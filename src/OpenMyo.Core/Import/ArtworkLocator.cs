namespace OpenMyo.Core.Import;

/// <summary>Finds cover art and per-track icon art for a folder of audio files.</summary>
public static class ArtworkLocator
{
    private static readonly string[] CoverNames = ["cover", "folder", "front", "album", "albumart", "artwork"];

    /// <summary>
    /// Cover preference: a conventionally named image in the folder, then the only image that is not a
    /// track sidecar, then art embedded in the first track that has some.
    /// </summary>
    public static ArtworkSource? FindCover(string folder, IReadOnlyList<ScannedFile> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);

        var images = ImagesIn(folder);
        var named = images.FirstOrDefault(image => CoverNames.Contains(BaseName(image), StringComparer.OrdinalIgnoreCase));
        if (named is not null)
        {
            return ArtworkSource.File(named);
        }

        var trackNames = tracks.Select(track => BaseName(track.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var loose = images.Where(image => !trackNames.Contains(BaseName(image))).ToList();
        if (loose.Count == 1)
        {
            return ArtworkSource.File(loose[0]);
        }

        var embedded = tracks.FirstOrDefault(track => track.Metadata.PictureHash is not null);
        return embedded is null ? null : ArtworkSource.Embedded(embedded.Path);
    }

    /// <summary>
    /// Icon art for each track (same order): an image with the same base name as the audio file,
    /// or embedded art when tracks carry <em>different</em> pictures (shared album art is a cover, not an icon).
    /// </summary>
    public static IReadOnlyList<ArtworkSource?> FindTrackIcons(string folder, IReadOnlyList<ScannedFile> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);

        var imagesByName = ImagesIn(folder)
            .GroupBy(BaseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var hasDistinctEmbeddedArt = tracks
            .Select(track => track.Metadata.PictureHash)
            .Where(hash => hash is not null)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1;

        return tracks.Select(track => IconFor(track, imagesByName, hasDistinctEmbeddedArt)).ToList();
    }

    private static ArtworkSource? IconFor(ScannedFile track, Dictionary<string, string> imagesByName, bool useEmbedded)
    {
        if (imagesByName.TryGetValue(BaseName(track.Path), out var sidecar))
        {
            return ArtworkSource.File(sidecar);
        }

        return useEmbedded && track.Metadata.PictureHash is not null ? ArtworkSource.Embedded(track.Path) : null;
    }

    private static List<string> ImagesIn(string folder) =>
        Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder).Where(MediaTypes.IsImage).Order(StringComparer.Ordinal).ToList()
            : [];

    private static string BaseName(string path) => Path.GetFileNameWithoutExtension(path);
}
