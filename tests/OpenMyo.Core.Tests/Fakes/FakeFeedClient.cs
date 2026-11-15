using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Tests.Fakes;

/// <summary>Serves canned feed XML and writes the given bytes for each download URL.</summary>
public sealed class FakeFeedClient : IFeedClient
{
    public string FeedXml { get; set; } = string.Empty;

    private int _running;

    public Dictionary<Uri, byte[]> Files { get; } = [];

    /// <summary>When set, each download waits this long so overlapping downloads can be counted.</summary>
    public TimeSpan DownloadDelay { get; set; }

    /// <summary>The most downloads that were running at the same moment.</summary>
    public int MostAtOnce { get; private set; }

    public Task<string> GetFeedAsync(Uri feedUrl, CancellationToken cancellationToken) => Task.FromResult(FeedXml);

    public async Task DownloadAsync(Uri source, string destinationPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Files.TryGetValue(source, out var content))
        {
            throw new HttpRequestException($"404 for {source}");
        }

        var running = Interlocked.Increment(ref _running);
        lock (Files)
        {
            MostAtOnce = Math.Max(MostAtOnce, running);
        }

        try
        {
            progress?.Report(0.5);
            await Task.Delay(DownloadDelay, cancellationToken);
            await File.WriteAllBytesAsync(destinationPath, content, cancellationToken);
            progress?.Report(1);
        }
        finally
        {
            Interlocked.Decrement(ref _running);
        }
    }
}
