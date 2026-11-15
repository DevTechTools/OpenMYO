using System.Collections.Concurrent;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;

namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// Makes a community icon usable on a card: downloads that one PNG and uploads it to the user's own Yoto
/// icons. Each icon is imported once per run; choosing it again reuses the upload.
/// </summary>
public sealed class CommunityIconImporter(ICommunityIconSource source, IMediaUploader uploader, IconIndexProvider indexProvider)
{
    private const string ContentType = "image/png";

    private readonly ConcurrentDictionary<string, Lazy<Task<DisplayIcon>>> _imports = new(StringComparer.Ordinal);

    public Task<DisplayIcon> ImportAsync(CommunityIcon icon, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(icon);

        var import = _imports.GetOrAdd(icon.Id, _ => new Lazy<Task<DisplayIcon>>(() => UploadAsync(icon, cancellationToken)));
        if (import.Value.IsFaulted || import.Value.IsCanceled)
        {
            _imports.TryRemove(icon.Id, out _);
        }

        return import.Value;
    }

    private async Task<DisplayIcon> UploadAsync(CommunityIcon icon, CancellationToken cancellationToken)
    {
        var image = await source.DownloadAsync(icon, cancellationToken);
        var mediaId = await uploader.UploadIconAsync(image, ContentType, $"{icon.Title} (yotoicons {icon.Id})", cancellationToken);
        indexProvider.Invalidate();
        return new DisplayIcon(mediaId, icon.Title, icon.Tags, ImageUrl: null, IsPublic: false);
    }
}
