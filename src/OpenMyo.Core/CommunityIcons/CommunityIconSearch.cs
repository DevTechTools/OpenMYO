using OpenMyo.Core.Text;

namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// Searches the community site's icons locally: the whole catalog is harvested ahead of time (see
/// <see cref="CommunityCatalogProvider"/>), so typing never sends requests to the site.
/// </summary>
public sealed class CommunityIconSearch(CommunityCatalogProvider catalog)
{
    /// <summary>
    /// Best trigram matches first, then popular icons containing a keyword. The first call may wait for the
    /// catalog to load or harvest.
    /// </summary>
    /// <exception cref="HttpRequestException">The site is unreachable and there is no cached catalog.</exception>
    public async Task<IReadOnlyList<CommunityIcon>> SearchAsync(string text, int maxResults, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (TextNormalizer.Keywords(text).Count == 0)
        {
            return []; // Nothing to search for; don't start a harvest for it.
        }

        var index = await catalog.GetAsync(progress: null, cancellationToken);
        return index.Search(text, maxResults);
    }

    /// <summary>The best match for <paramref name="text"/> scoring at least <paramref name="minScore"/> (0 to 1), or null.</summary>
    /// <exception cref="HttpRequestException">The site is unreachable and there is no cached catalog.</exception>
    public async Task<CommunityIcon?> BestMatchAsync(string text, double minScore, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        var index = await catalog.GetAsync(progress: null, cancellationToken);
        return index.BestMatch(text, minScore);
    }

    /// <summary>The icon with the site's <paramref name="id"/>, or null when the catalog has none.</summary>
    /// <exception cref="HttpRequestException">The site is unreachable and there is no cached catalog.</exception>
    public async Task<CommunityIcon?> FindAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var index = await catalog.GetAsync(progress: null, cancellationToken);
        return index.Find(id);
    }
}
