namespace OpenMyo.Core.Abstractions;

/// <summary>Uploads local media to Yoto.</summary>
public interface IMediaUploader
{
    /// <summary>Uploads an audio file and waits until Yoto has transcoded it.</summary>
    /// <param name="filePath">Local audio file.</param>
    /// <param name="progress">Receives values from 0 to 1.</param>
    Task<UploadedAudio> UploadAudioAsync(string filePath, IProgress<double>? progress, CancellationToken cancellationToken);

    /// <summary>Uploads a cover image and returns its public URL.</summary>
    Task<Uri> UploadCoverAsync(ReadOnlyMemory<byte> image, string contentType, CancellationToken cancellationToken);

    /// <summary>Uploads an image as a 16x16 display icon (Yoto resizes it) and returns its media id.</summary>
    Task<string> UploadIconAsync(ReadOnlyMemory<byte> image, string contentType, string fileName, CancellationToken cancellationToken);
}

/// <summary>Result of a completed audio upload, ready to place in a track.</summary>
/// <param name="TrackUrl">Reference such as <c>yoto:#sha256</c>.</param>
public sealed record UploadedAudio(string TrackUrl, string Format, TimeSpan Duration, long FileSize, string? Channels);
