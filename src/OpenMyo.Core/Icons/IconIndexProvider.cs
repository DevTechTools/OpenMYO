using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Builds the <see cref="IconIndex"/> once from the catalog plus icons already used in the library, and shares it;
/// rebuilt on demand. Catalog entries win when both know an icon, because their tags are curated.
/// </summary>
public sealed class IconIndexProvider(IIconCatalog catalog)
{
    private readonly Lock _gate = new();
    private Task<IconIndex>? _index;
    private IReadOnlyList<DisplayIcon> _libraryIcons = [];

    public Task<IconIndex> GetAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_index is null || _index.IsFaulted || _index.IsCanceled)
            {
                _index = BuildAsync(cancellationToken);
            }

            return _index;
        }
    }

    /// <summary>Forces the next <see cref="GetAsync"/> to rebuild, e.g. after uploading a custom icon.</summary>
    public void Invalidate()
    {
        lock (_gate)
        {
            _index = null;
        }
    }

    /// <summary>Adds the icons found in the user's playlists (see <see cref="LibraryIcons"/>) and rebuilds the index.</summary>
    public void SetLibraryIcons(IReadOnlyList<DisplayIcon> icons)
    {
        ArgumentNullException.ThrowIfNull(icons);
        lock (_gate)
        {
            _libraryIcons = icons;
            _index = null;
        }
    }

    private async Task<IconIndex> BuildAsync(CancellationToken cancellationToken)
    {
        var library = _libraryIcons;
        var catalogIcons = await catalog.GetIconsAsync(cancellationToken);
        var icons = catalogIcons.Concat(library).DistinctBy(icon => icon.MediaId, StringComparer.Ordinal).ToList();
        return await Task.Run(() => IconIndex.Build(icons), cancellationToken);
    }
}
