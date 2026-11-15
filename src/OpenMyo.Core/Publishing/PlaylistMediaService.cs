using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Publishing;

/// <summary>Media changes to an existing playlist: adding local tracks and replacing the cover.</summary>
public sealed class PlaylistMediaService(DraftBuilder draftBuilder, PlaylistPublisher publisher, IMediaUploader uploader)
{
    /// <summary>Uploads the files (cleaned and ordered like an import) and appends them as new chapters.</summary>
    public async Task<Playlist> AppendFilesAsync(
        Playlist playlist, IReadOnlyList<string> filePaths, IProgress<PublishProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        ArgumentNullException.ThrowIfNull(filePaths);

        var audio = filePaths.Where(MediaTypes.IsAudio).ToList();
        if (audio.Count == 0)
        {
            return playlist;
        }

        var folder = Path.GetDirectoryName(audio[0]) ?? string.Empty;
        var draft = await draftBuilder.BuildAsync(new ImportGroup(folder, audio), cancellationToken);
        return await publisher.AppendFilesAsync(playlist, draft.Tracks, progress, cancellationToken);
    }

    /// <summary>Uploads an image file and returns the playlist with the new cover (not yet saved).</summary>
    public async Task<Playlist> ReplaceCoverAsync(Playlist playlist, string imagePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var url = await uploader.UploadCoverAsync(bytes, MediaTypes.ContentTypeOf(imagePath), cancellationToken);
        return playlist with { CoverImageUrl = url };
    }
}
