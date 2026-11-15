using System.Net.Http.Headers;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Feeds;

namespace OpenMyo.Infrastructure.Feeds;

/// <summary>Fetches podcast feeds and downloads their audio to disk.</summary>
public sealed class HttpFeedClient(IHttpClientFactory httpClientFactory) : IFeedClient
{
    /// <summary>Named client registered in <see cref="ServiceCollectionExtensions.AddInfrastructure"/>.</summary>
    public const string HttpClientName = "Feeds";

    // Some podcast hosts refuse requests without a user agent. Kept generic: nothing about the user.
    public static readonly ProductInfoHeaderValue UserAgent = new("OpenMyo", "1.0");

    // Very large back catalogues reach a few megabytes; anything far bigger is not a feed.
    private const long MaxFeedBytes = 32L * 1024 * 1024;
    private static readonly TimeSpan FeedTimeout = TimeSpan.FromSeconds(30);
    private const int CopyBufferSize = 81920;

    public async Task<string> GetFeedAsync(Uri feedUrl, CancellationToken cancellationToken)
    {
        RequireWeb(feedUrl);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(FeedTimeout);
        using var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new FeedFormatException($"That feed address returned an error ({(int)response.StatusCode} {response.ReasonPhrase}). Check the link and try again.");
        }

        if (response.Content.Headers.ContentLength > MaxFeedBytes)
        {
            throw new FeedFormatException("That address returned a file far too large to be a feed.");
        }

        return await response.Content.ReadAsStringAsync(timeout.Token);
    }

    public async Task DownloadAsync(Uri source, string destinationPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        RequireWeb(source);
        using var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        EnsureFound(response);

        var length = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
        var buffer = new byte[CopyBufferSize];
        long written = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            written += read;
            if (length > 0)
            {
                progress?.Report((double)written / length.Value);
            }
        }
    }

    private static void RequireWeb(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!FeedUrl.IsWeb(url))
        {
            throw new FeedFormatException("Only web (http or https) addresses can be imported.");
        }
    }

    private static void EnsureFound(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The server answered {(int)response.StatusCode} ({response.ReasonPhrase}).", inner: null, response.StatusCode);
        }
    }
}
