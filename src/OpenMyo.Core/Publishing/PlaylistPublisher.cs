using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Import;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Publishing;

/// <summary>Uploads a draft's media and creates (or extends) the Yoto playlist.</summary>
public sealed class PlaylistPublisher(
    IMediaUploader uploader,
    IPlaylistRepository repository,
    ISettingsStore settingsStore,
    ArtworkLoader artworkLoader,
    IconIndexProvider iconIndexProvider)
{
    /// <summary>Creates a new playlist from a draft.</summary>
    public async Task<Playlist> PublishAsync(PlaylistDraft draft, IProgress<PublishProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var chapters = await UploadChaptersAsync(draft.Tracks, startKey: 1, progress, cancellationToken);
        var cover = await UploadCoverAsync(draft.Cover, cancellationToken);

        progress?.Report(new PublishProgress(0.99, "Saving playlist"));
        var playlist = new Playlist { Title = draft.Title, CoverImageUrl = cover, Chapters = chapters };
        return await repository.SaveAsync(playlist, cancellationToken);
    }

    /// <summary>Appends local files to an existing playlist.</summary>
    public async Task<Playlist> AppendFilesAsync(
        Playlist playlist, IReadOnlyList<TrackDraft> tracks, IProgress<PublishProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        var added = await UploadChaptersAsync(tracks, playlist.Chapters.Count + 1, progress, cancellationToken);
        progress?.Report(new PublishProgress(0.99, "Saving playlist"));
        return await repository.SaveAsync(playlist with { Chapters = [.. playlist.Chapters, .. added] }, cancellationToken);
    }

    private async Task<IReadOnlyList<Chapter>> UploadChaptersAsync(
        IReadOnlyList<TrackDraft> tracks, int startKey, IProgress<PublishProgress>? progress, CancellationToken cancellationToken)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        var tracker = new UploadProgressTracker(tracks.Count, progress);
        var chapters = new Chapter[tracks.Count];
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Clamp(settings.MaxParallelUploads, 1, 8),
        };

        await Parallel.ForEachAsync(Enumerable.Range(0, tracks.Count), options, async (i, token) =>
        {
            var key = ChapterKey(startKey + i);
            chapters[i] = await UploadChapterAsync(tracks[i], key, tracker.ForFile(i), token);
        });

        if (tracks.Any(track => track.IconMediaId is null && track.IconArtwork is not null))
        {
            iconIndexProvider.Invalidate();
        }

        return chapters;
    }

    private async Task<Chapter> UploadChapterAsync(TrackDraft draft, string key, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var audio = await uploader.UploadAudioAsync(draft.SourcePath, progress, cancellationToken);
        var iconId = draft.IconMediaId ?? await UploadIconAsync(draft, cancellationToken);
        var track = new Track
        {
            Key = key,
            Title = draft.Title,
            TrackUrl = audio.TrackUrl,
            Format = audio.Format,
            Duration = audio.Duration,
            FileSize = audio.FileSize,
            Channels = audio.Channels,
            IconMediaId = iconId,
        };
        return new Chapter { Key = key, Title = draft.Title, IconMediaId = iconId, Tracks = [track] };
    }

    private async Task<string?> UploadIconAsync(TrackDraft draft, CancellationToken cancellationToken)
    {
        if (draft.IconArtwork is null)
        {
            return null;
        }

        var picture = await artworkLoader.LoadAsync(draft.IconArtwork, cancellationToken);
        var fileName = Path.GetFileNameWithoutExtension(draft.SourcePath);
        return picture is null ? null : await uploader.UploadIconAsync(picture.Data, picture.ContentType, fileName, cancellationToken);
    }

    private async Task<Uri?> UploadCoverAsync(ArtworkSource? cover, CancellationToken cancellationToken)
    {
        var picture = cover is null ? null : await artworkLoader.LoadAsync(cover, cancellationToken);
        return picture is null ? null : await uploader.UploadCoverAsync(picture.Data, picture.ContentType, cancellationToken);
    }

    private static string ChapterKey(int number) => number.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
}
