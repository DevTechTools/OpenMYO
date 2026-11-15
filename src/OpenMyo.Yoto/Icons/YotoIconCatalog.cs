using System.Text.Json;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;
using OpenMyo.Yoto.Http;

namespace OpenMyo.Yoto.Icons;

/// <summary>
/// Yoto's public icons (disk cache, 7 days) plus the user's own uploads (memory only, invalidated after an
/// upload). Concurrent callers share one in-flight load per source.
/// </summary>
internal sealed class YotoIconCatalog(IHttpClientFactory httpClientFactory, IAppPaths paths) : IIconCatalog
{
    private const string PublicPath = "media/displayIcons/user/yoto";
    private const string UserPath = "media/displayIcons/user/me";
    private static readonly TimeSpan PublicCacheLifetime = TimeSpan.FromDays(7);

    private readonly object _gate = new();
    private Task<IReadOnlyList<DisplayIcon>>? _public;
    private Task<IReadOnlyList<DisplayIcon>>? _user;

    public async Task<IReadOnlyList<DisplayIcon>> GetIconsAsync(CancellationToken cancellationToken)
    {
        Task<IReadOnlyList<DisplayIcon>> publicLoad;
        Task<IReadOnlyList<DisplayIcon>> userLoad;
        lock (_gate)
        {
            publicLoad = _public = ReuseUnlessFailed(_public, () => LoadPublicAsync());
            userLoad = _user = ReuseUnlessFailed(_user, () => DownloadIconListAsync(UserPath, isPublic: false, CancellationToken.None));
        }

        var shared = await publicLoad.WaitAsync(cancellationToken);
        var own = await userLoad.WaitAsync(cancellationToken);
        return shared.Concat(own).DistinctBy(icon => icon.MediaId, StringComparer.Ordinal).ToList();
    }

    /// <summary>Forgets the user's icons so the next call sees a fresh upload.</summary>
    public void InvalidateUserIcons()
    {
        lock (_gate)
        {
            _user = null;
        }
    }

    private static Task<IReadOnlyList<DisplayIcon>> ReuseUnlessFailed(Task<IReadOnlyList<DisplayIcon>>? current, Func<Task<IReadOnlyList<DisplayIcon>>> load) =>
        current is { IsFaulted: false, IsCanceled: false } ? current : load();

    private async Task<IReadOnlyList<DisplayIcon>> LoadPublicAsync()
    {
        var file = Path.Combine(paths.CacheDirectory, "icons", "public.json");
        var cached = await TryReadCacheAsync(file);
        if (cached is { Fresh: true })
        {
            return cached.Icons;
        }

        try
        {
            var json = await DownloadAsync(PublicPath, CancellationToken.None);
            await TryWriteCacheAsync(file, json);
            return IconMapper.ParseIconList(json, isPublic: true);
        }
        catch (Exception ex) when (cached is not null && ex is HttpRequestException or YotoApiException or JsonException)
        {
            return cached.Icons; // Stale icons beat no icons when offline.
        }
    }

    private async Task<IReadOnlyList<DisplayIcon>> DownloadIconListAsync(string path, bool isPublic, CancellationToken cancellationToken)
    {
        var json = await DownloadAsync(path, cancellationToken);
        return IconMapper.ParseIconList(json, isPublic);
    }

    private async Task<string> DownloadAsync(string path, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var response = await client.GetAsync(path, cancellationToken);
        await YotoResponse.EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static async Task<CachedIcons?> TryReadCacheAsync(string file)
    {
        try
        {
            if (!File.Exists(file))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(file);
            var icons = IconMapper.ParseIconList(json, isPublic: true);
            return new CachedIcons(icons, DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < PublicCacheLifetime);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static async Task TryWriteCacheAsync(string file, string json)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!); // The path always has a directory.
            await File.WriteAllTextAsync(file, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Caching is best effort.
        }
    }

    private sealed record CachedIcons(IReadOnlyList<DisplayIcon> Icons, bool Fresh);
}
