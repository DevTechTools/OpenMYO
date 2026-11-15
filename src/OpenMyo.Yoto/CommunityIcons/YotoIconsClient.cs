using System.Globalization;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Yoto.CommunityIcons;

/// <summary>
/// Reads yotoicons.com, a free community site with no API. To stay a polite guest it sends at most four
/// requests at a time; the harvested catalog is cached by <see cref="CommunityCatalogFile"/>.
/// </summary>
internal sealed class YotoIconsClient(IHttpClientFactory httpClientFactory) : ICommunityIconSource, IDisposable
{
    public const string HttpClientName = "yotoicons";
    public static readonly Uri BaseAddress = new("https://yotoicons.com/");

    private const int MaxConcurrentRequests = 4;
    private readonly SemaphoreSlim _gate = new(MaxConcurrentRequests, MaxConcurrentRequests);

    public async Task<CommunityCatalogPage> GetPageAsync(int page, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        var path = string.Create(CultureInfo.InvariantCulture, $"icons?sort=newest&type=singles&page={page}");
        var html = await SendAsync((client, token) => client.GetStringAsync(path, token), cancellationToken);
        return YotoIconsListingParser.ParsePage(html);
    }

    public async Task<ReadOnlyMemory<byte>> DownloadAsync(CommunityIcon icon, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(icon);
        if (!string.Equals(icon.ImageUrl.Host, BaseAddress.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Community icons are only downloaded from yotoicons.com.", nameof(icon));
        }

        return await SendAsync((client, token) => client.GetByteArrayAsync(icon.ImageUrl, token), cancellationToken);
    }

    public void Dispose() => _gate.Dispose();

    // A client timeout surfaces as TaskCanceledException; report it as the site being unreachable instead,
    // so callers (and the harvester's retries) treat it like any other network failure.
    private async Task<T> SendAsync<T>(Func<HttpClient, CancellationToken, Task<T>> send, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            return await send(client, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpRequestException("yotoicons.com did not answer in time.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }
}
