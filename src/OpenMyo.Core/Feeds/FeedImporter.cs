using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Feeds;

/// <summary>
/// Imports a podcast feed: reads it for preview, then downloads the chosen episodes and the feed's artwork into
/// a temporary cache folder and returns a draft ready for review. The folder is removed with <see cref="Discard"/>
/// once the draft is published or dropped; leftovers are swept on the next download.
/// </summary>
public sealed class FeedImporter(IFeedClient client, IAudioMetadataReader metadataReader, IAppPaths paths, ISettingsStore settingsStore)
{
    /// <summary>A Yoto Make Your Own card holds at most this many tracks.</summary>
    public const int MaxEpisodes = 100;

    private const string DownloadsFolderName = "FeedDownloads";
    /// <summary>Upper bound on the user's "downloaded at the same time" setting, to stay polite to podcast hosts.</summary>
    private const int ParallelDownloadsLimit = 8;
    private const int ImageHeaderLength = 8;
    private static readonly TimeSpan LeftoverAge = TimeSpan.FromDays(2);

    private string DownloadsRoot => Path.Combine(paths.CacheDirectory, DownloadsFolderName);

    public async Task<PodcastFeed> LoadAsync(Uri feedUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(feedUrl);
        var xml = await client.GetFeedAsync(feedUrl, cancellationToken);
        return FeedParser.Parse(xml, feedUrl);
    }

    /// <summary>Downloads <paramref name="selected"/>, as tracks in <paramref name="order"/>, and builds a draft from them.</summary>
    public async Task<PlaylistDraft> DownloadAsync(
        PodcastFeed feed,
        IReadOnlyCollection<FeedEpisode> selected,
        EpisodeOrder order,
        IProgress<FeedDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(selected);
        var inFeedOrder = feed.Episodes.Where(selected.Contains);
        var episodes = (order == EpisodeOrder.NewestFirst ? inFeedOrder.Reverse() : inFeedOrder).ToList();
        if (episodes.Count is 0 or > MaxEpisodes)
        {
            throw new ArgumentException($"Choose between 1 and {MaxEpisodes} episodes.", nameof(selected));
        }

        DeleteLeftovers();
        var folder = Directory.CreateDirectory(Path.Combine(DownloadsRoot, Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var files = await DownloadEpisodesAsync(episodes, folder, progress, cancellationToken);
            progress?.Report(new FeedDownloadProgress(1, "Downloading artwork"));
            var cover = await DownloadCoverAsync(feed.ImageUrl, folder, cancellationToken);
            return BuildDraft(feed, episodes, files, folder, cover);
        }
        catch
        {
            TryDelete(folder);
            throw;
        }
    }

    /// <summary>Deletes the downloaded files behind a feed draft. Does nothing for drafts made from the user's own files.</summary>
    public void Discard(PlaylistDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(draft.SourceFolder));
        if (string.Equals(parent, Path.TrimEndingDirectorySeparator(DownloadsRoot), StringComparison.Ordinal))
        {
            TryDelete(draft.SourceFolder);
        }
    }

    private async Task<string[]> DownloadEpisodesAsync(
        List<FeedEpisode> episodes, string folder, IProgress<FeedDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        var files = episodes.Select((episode, i) => Path.Combine(folder, FeedFileNames.ForEpisode(episode, i))).ToArray();
        var tracker = new DownloadProgressTracker(episodes, progress);
        var settings = await settingsStore.LoadAsync(cancellationToken);
        await Parallel.ForEachAsync(
            Enumerable.Range(0, episodes.Count),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Clamp(settings.MaxParallelDownloads, 1, ParallelDownloadsLimit),
            },
            async (i, token) =>
            {
                var episodeProgress = tracker.ForEpisode(i);
                await client.DownloadAsync(episodes[i].AudioUrl, files[i], episodeProgress, token);
                episodeProgress.Report(1);
            });
        return files;
    }

    /// <summary>Artwork is a nice-to-have: a broken image link must not lose the downloaded episodes.</summary>
    private async Task<ArtworkSource?> DownloadCoverAsync(Uri? imageUrl, string folder, CancellationToken cancellationToken)
    {
        if (imageUrl is null)
        {
            return null;
        }

        var temporary = Path.Combine(folder, "cover.download");
        try
        {
            await client.DownloadAsync(imageUrl, temporary, progress: null, cancellationToken);
        }
        catch (HttpRequestException)
        {
            TryDeleteFile(temporary);
            return null;
        }

        var cover = Path.Combine(folder, FeedFileNames.ForCover(await ReadHeaderAsync(temporary, cancellationToken)));
        File.Move(temporary, cover);
        return ArtworkSource.File(cover);
    }

    private PlaylistDraft BuildDraft(PodcastFeed feed, List<FeedEpisode> episodes, string[] files, string folder, ArtworkSource? cover)
    {
        var titles = TitleCleaner.CleanAll(episodes.Select(episode => episode.Title).ToList());
        var tracks = episodes.Select((episode, i) =>
        {
            var duration = metadataReader.Read(files[i]).Duration;
            return new TrackDraft(files[i], titles[i], duration > TimeSpan.Zero ? duration : episode.Duration ?? TimeSpan.Zero);
        }).ToList();
        return new PlaylistDraft(TitleCleaner.Clean(feed.Title), folder, tracks, cover, $"From feed {feed.Url}");
    }

    private static async Task<byte[]> ReadHeaderAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var header = new byte[ImageHeaderLength];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        return header[..read];
    }

    private void DeleteLeftovers()
    {
        if (!Directory.Exists(DownloadsRoot))
        {
            return;
        }

        var cutoff = DateTime.UtcNow - LeftoverAge;
        foreach (var folder in Directory.EnumerateDirectories(DownloadsRoot).Where(folder => Directory.GetLastWriteTimeUtc(folder) < cutoff))
        {
            TryDelete(folder);
        }
    }

    // Cleanup is best effort: a file still open in the player is removed on a later sweep instead.
    private static void TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
