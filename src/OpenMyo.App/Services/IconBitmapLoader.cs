using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Import;
using OpenMyo.Core.Publishing;
using OpenMyo.Yoto.Icons;

namespace OpenMyo.App.Services;

/// <summary>
/// Resolves icon media ids and local artwork into bitmaps for display. Never throws: a picture that can't load
/// is logged and shown as missing, so rows and tiles can start loads without awaiting them.
/// </summary>
public sealed class IconBitmapLoader(
    IconIndexProvider indexProvider, RemoteImageCache imageCache, ArtworkLoader artworkLoader, ILogger<IconBitmapLoader> logger)
{
    private const int ThumbnailWidth = 160;

    public Task<Bitmap?> LoadIconAsync(string? mediaId) => string.IsNullOrEmpty(mediaId)
        ? Task.FromResult<Bitmap?>(null)
        : LoadOrLogAsync($"icon {mediaId}", async () =>
        {
            var index = await indexProvider.GetAsync(CancellationToken.None);
            var url = index.Find(mediaId)?.ImageUrl ?? YotoIconUrls.For(mediaId);
            return url is null ? null : await imageCache.GetAsync(url);
        });

    public Task<Bitmap?> LoadIconAsync(DisplayIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        var url = icon.ImageUrl ?? YotoIconUrls.For(icon.MediaId);
        return url is null ? Task.FromResult<Bitmap?>(null) : LoadOrLogAsync($"icon {icon.MediaId}", () => imageCache.GetAsync(url));
    }

    public Task<Bitmap?> LoadCoverAsync(Uri? url) =>
        url is null ? Task.FromResult<Bitmap?>(null) : LoadOrLogAsync("cover", () => imageCache.GetAsync(url, ThumbnailWidth));

    /// <summary>Decodes local artwork (an image file or art embedded in audio) as a thumbnail.</summary>
    public Task<Bitmap?> LoadArtworkAsync(ArtworkSource? source) => source is null
        ? Task.FromResult<Bitmap?>(null)
        : LoadOrLogAsync("local artwork", async () =>
        {
            var picture = await artworkLoader.LoadAsync(source, CancellationToken.None);
            if (picture is null)
            {
                return null;
            }

            using var stream = new MemoryStream(picture.Data.ToArray());
            return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
        });

    private async Task<Bitmap?> LoadOrLogAsync(string what, Func<Task<Bitmap?>> load)
    {
        try
        {
            return await load();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // UI boundary: a picture that fails to load must never break the screen showing it.
            logger.LogWarning(exception, "Could not load {Image}", what);
            return null;
        }
    }
}
