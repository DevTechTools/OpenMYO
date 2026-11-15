using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.App.Services;

/// <summary>
/// Downloads images once, keeps the bytes on disk and the decoded bitmaps in memory.
/// Concurrent requests for the same URL share one download.
/// </summary>
public sealed class RemoteImageCache(IHttpClientFactory httpClientFactory, IAppPaths paths, ILogger<RemoteImageCache> logger)
{
    private readonly ConcurrentDictionary<string, Lazy<Task<Bitmap?>>> _bitmaps = new(StringComparer.Ordinal);

    /// <param name="decodeWidth">Decode to this width to save memory (covers); null keeps native size (icons).</param>
    public Task<Bitmap?> GetAsync(Uri url, int? decodeWidth = null)
    {
        ArgumentNullException.ThrowIfNull(url);

        var key = $"{url.AbsoluteUri}|{decodeWidth}";
        var entry = _bitmaps.GetOrAdd(key, _ => new Lazy<Task<Bitmap?>>(() => LoadAsync(url, decodeWidth)));
        if (entry.Value.IsFaulted)
        {
            _bitmaps.TryRemove(key, out _);
        }

        return entry.Value;
    }

    private async Task<Bitmap?> LoadAsync(Uri url, int? decodeWidth)
    {
        try
        {
            var file = CachePathFor(url);
            if (!File.Exists(file))
            {
                await DownloadAsync(url, file);
            }

            await using var stream = File.OpenRead(file);
            return decodeWidth is { } width ? Bitmap.DecodeToWidth(stream, width) : new Bitmap(stream);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Could not load image from {Host}", url.Host);
            }

            return null;
        }
    }

    private async Task DownloadAsync(Uri url, string file)
    {
        using var client = httpClientFactory.CreateClient(nameof(RemoteImageCache));
        var bytes = await client.GetByteArrayAsync(url);
        var temporary = file + ".tmp";
        await File.WriteAllBytesAsync(temporary, bytes);
        File.Move(temporary, file, overwrite: true);
    }

    private string CachePathFor(Uri url)
    {
        var folder = Path.Combine(paths.CacheDirectory, "images");
        Directory.CreateDirectory(folder);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(url.AbsoluteUri)));
        return Path.Combine(folder, hash);
    }
}
