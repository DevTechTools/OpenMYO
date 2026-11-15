using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Import;

/// <summary>
/// Turns selected files into ready-to-review playlist drafts: reads tags in parallel, orders tracks,
/// cleans titles and locates artwork. No network access.
/// </summary>
public sealed class DraftBuilder(IAudioMetadataReader metadataReader)
{
    public async Task<IReadOnlyList<PlaylistDraft>> BuildAsync(IEnumerable<string> selectedPaths, CancellationToken cancellationToken)
    {
        var groups = await Task.Run(() => ImportGrouper.Group(selectedPaths), cancellationToken);
        var drafts = new List<PlaylistDraft>(groups.Count);
        foreach (var group in groups)
        {
            drafts.Add(await BuildAsync(group, cancellationToken));
        }

        return drafts;
    }

    public async Task<PlaylistDraft> BuildAsync(ImportGroup group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);

        var scanned = await ScanAsync(group.AudioFiles, cancellationToken);
        var ordered = TrackOrderer.Order(scanned);
        var titles = TitleCleaner.CleanAll(ordered.Select(PreferredRawTitle).ToList());
        var icons = ArtworkLocator.FindTrackIcons(group.Folder, ordered);

        var tracks = ordered
            .Select((file, i) => new TrackDraft(file.Path, titles[i], file.Metadata.Duration, icons[i]))
            .ToList();
        return new PlaylistDraft(PlaylistTitle(group.Folder, ordered), group.Folder, tracks, ArtworkLocator.FindCover(group.Folder, ordered));
    }

    private async Task<IReadOnlyList<ScannedFile>> ScanAsync(IReadOnlyList<string> files, CancellationToken cancellationToken)
    {
        var results = new ScannedFile[files.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, files.Count),
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount },
            (i, _) =>
            {
                results[i] = new ScannedFile(files[i], metadataReader.Read(files[i]));
                return ValueTask.CompletedTask;
            });
        return results;
    }

    /// <summary>Tag titles are usually cleaner than file names, unless they are placeholders like "Track 01".</summary>
    private static string PreferredRawTitle(ScannedFile file)
    {
        var tagTitle = file.Metadata.Title?.Trim();
        return tagTitle is null || IsPlaceholderTitle(tagTitle) ? Path.GetFileNameWithoutExtension(file.Path) : tagTitle;
    }

    private static bool IsPlaceholderTitle(string title) =>
        title.Length == 0
        || (title.Length <= 9 && title.StartsWith("track", StringComparison.OrdinalIgnoreCase) && title.Any(char.IsDigit));

    /// <summary>The album tag when every track agrees on it, otherwise the folder name.</summary>
    private static string PlaylistTitle(string folder, IReadOnlyList<ScannedFile> files)
    {
        var albums = files.Select(file => file.Metadata.Album?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var raw = albums is [{ Length: > 0 } album] ? album : Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar));
        return TitleCleaner.Clean(string.IsNullOrWhiteSpace(raw) ? "New Playlist" : raw);
    }
}
