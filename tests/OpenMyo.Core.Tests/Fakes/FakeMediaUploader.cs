using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Tests.Fakes;

public sealed record UploadedIcon(string FileName, string ContentType, int Length);

public sealed class FakeMediaUploader : IMediaUploader
{
    public static readonly Uri CoverUrl = new("https://example.test/cover.png");

    private readonly Lock _gate = new();

    /// <summary>Audio paths whose upload finishes late, to prove chapter order does not depend on completion order.</summary>
    public HashSet<string> SlowPaths { get; } = new(StringComparer.Ordinal);

    public List<string> AudioPaths { get; } = [];

    public List<UploadedIcon> Icons { get; } = [];

    public List<(string ContentType, int Length)> Covers { get; } = [];

    public async Task<UploadedAudio> UploadAudioAsync(string filePath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (SlowPaths.Contains(filePath))
        {
            await Task.Delay(100, cancellationToken);
        }

        lock (_gate)
        {
            AudioPaths.Add(filePath);
        }

        progress?.Report(1);
        return new UploadedAudio("yoto:#" + Path.GetFileName(filePath), "mp3", TimeSpan.FromSeconds(10), 100, "stereo");
    }

    public Task<Uri> UploadCoverAsync(ReadOnlyMemory<byte> image, string contentType, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Covers.Add((contentType, image.Length));
        }

        return Task.FromResult(CoverUrl);
    }

    public Task<string> UploadIconAsync(ReadOnlyMemory<byte> image, string contentType, string fileName, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Icons.Add(new UploadedIcon(fileName, contentType, image.Length));
        }

        return Task.FromResult("uploaded-" + fileName);
    }
}
