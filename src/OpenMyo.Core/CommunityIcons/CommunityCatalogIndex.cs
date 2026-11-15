using OpenMyo.Core.Icons;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// The whole community catalog with a trigram index over it, built once per load so each search is local
/// and instant. Ranks like Yoto's own icons, then fills with popular icons that literally contain a keyword.
/// </summary>
public sealed class CommunityCatalogIndex
{
    private readonly IconIndex _index;
    private readonly Dictionary<string, CommunityIcon> _byId;

    private CommunityCatalogIndex(CommunityCatalog catalog, IconIndex index, Dictionary<string, CommunityIcon> byId)
    {
        Catalog = catalog;
        _index = index;
        _byId = byId;
    }

    private const int BestCandidates = 100;
    private const int ScoreDecimals = 3;

    public CommunityCatalog Catalog { get; }

    public int Count => _byId.Count;

    public static CommunityCatalogIndex Build(CommunityCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var byId = catalog.Icons.DistinctBy(icon => icon.Id, StringComparer.Ordinal).ToDictionary(icon => icon.Id, StringComparer.Ordinal);
        var index = IconIndex.Build(byId.Values.Select(ToDisplayIcon));
        return new CommunityCatalogIndex(catalog, index, byId);
    }

    /// <summary>The icon with the site's <paramref name="id"/>, or null when the catalog has none.</summary>
    public CommunityIcon? Find(string id) => _byId.GetValueOrDefault(id);

    public IReadOnlyList<CommunityIcon> Search(string text, int maxResults)
    {
        ArgumentNullException.ThrowIfNull(text);

        var keywords = TextNormalizer.Keywords(text).Distinct(StringComparer.Ordinal).ToList();
        if (keywords.Count == 0 || maxResults <= 0)
        {
            return [];
        }

        var ranked = _index.Search(keywords, maxResults).Select(match => _byId[match.Icon.MediaId]).ToList();
        if (ranked.Count >= maxResults)
        {
            return ranked;
        }

        var seen = ranked.Select(icon => icon.Id).ToHashSet(StringComparer.Ordinal);
        var popular = _byId.Values
            .Where(icon => !seen.Contains(icon.Id) && ContainsAny(icon, keywords))
            .OrderByDescending(icon => icon.Downloads)
            .Take(maxResults - ranked.Count);
        return ranked.Concat(popular).ToList();
    }

    /// <summary>
    /// The best icon for <paramref name="text"/> when it scores at least <paramref name="minScore"/>; otherwise
    /// null. The trigram score is weighted by focus (how much of the icon's own wording the title matched), so
    /// "fence" beats a song lyric that happens to contain "fence". Ties go to the most downloaded icon.
    /// </summary>
    public CommunityIcon? BestMatch(string text, double minScore)
    {
        ArgumentNullException.ThrowIfNull(text);

        var keywords = TextNormalizer.Keywords(text).Distinct(StringComparer.Ordinal).ToList();
        if (keywords.Count == 0)
        {
            return null;
        }

        return _index.Search(keywords, BestCandidates)
            .Select(match => (Icon: _byId[match.Icon.MediaId], Score: match.Score * FocusWeight(_byId[match.Icon.MediaId], keywords)))
            .Where(candidate => candidate.Score >= minScore)
            .OrderByDescending(candidate => Math.Round(candidate.Score, ScoreDecimals))
            .ThenByDescending(candidate => candidate.Icon.Downloads)
            .Select(candidate => candidate.Icon)
            .FirstOrDefault();
    }

    // 1 when every word of the icon matched the title (directly, or as a synonym), falling with the share that
    // did not; the square root keeps a two-word icon matching one word ("rainbow end") in contention.
    // An exact word match is required: similar spelling alone ("dragonfly" for "dragon") scores 0.
    private static double FocusWeight(CommunityIcon icon, IReadOnlyList<string> keywords)
    {
        // The title is the icon's two tags ("fence · garden"); the remaining tag is only its site category.
        var words = TextNormalizer.Keywords(icon.Title);
        var matched = words.Count(word => keywords.Any(keyword => keyword == word || WordSynonyms.For(keyword).Contains(word)));
        return matched == 0 ? 0 : Math.Sqrt((double)matched / words.Count);
    }

    // MediaId carries the site's id so a match maps straight back to its community icon.
    private static DisplayIcon ToDisplayIcon(CommunityIcon icon) => new(icon.Id, icon.Title, icon.Tags, icon.ImageUrl, IsPublic: false);

    private static bool ContainsAny(CommunityIcon icon, IReadOnlyList<string> keywords) =>
        keywords.Any(keyword => icon.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || icon.Tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
}
