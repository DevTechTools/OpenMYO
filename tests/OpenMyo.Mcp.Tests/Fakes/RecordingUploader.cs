using OpenMyo.Core.Abstractions;

namespace OpenMyo.Mcp.Tests.Fakes;

public sealed class RecordingUploader : IMediaUploader
{
    public List<string> IconNames { get; } = [];

    public Task<UploadedAudio> UploadAudioAsync(string filePath, IProgress<double>? progress, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<Uri> UploadCoverAsync(ReadOnlyMemory<byte> image, string contentType, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<string> UploadIconAsync(ReadOnlyMemory<byte> image, string contentType, string fileName, CancellationToken cancellationToken)
    {
        IconNames.Add(fileName);
        return Task.FromResult("uploaded-" + IconNames.Count);
    }
}
