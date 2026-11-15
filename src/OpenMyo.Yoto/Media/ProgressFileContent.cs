using System.Net;

namespace OpenMyo.Yoto.Media;

/// <summary>Streams a file as an HTTP body in chunks (never fully in memory) and reports how much was sent.</summary>
internal sealed class ProgressFileContent : HttpContent
{
    private const int BufferSize = 81920;

    private readonly string _filePath;
    private readonly long _length;
    private readonly Action<double>? _onProgress;

    public ProgressFileContent(string filePath, string contentType, Action<double>? onProgress)
    {
        _filePath = filePath;
        _length = new FileInfo(filePath).Length;
        _onProgress = onProgress;
        Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        await SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        var buffer = new byte[BufferSize];
        long sent = 0;
        int read;
        while ((read = await file.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sent += read;
            _onProgress?.Invoke(_length == 0 ? 1 : (double)sent / _length);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length;
        return true;
    }
}
