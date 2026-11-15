using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Finds yotoicons.com icons for titles Yoto's own catalog could not match, and adds them to the user's Yoto
/// icons. A community icon is used only when it matches a title better than Yoto's best did, and at least
/// one keyword matches solidly; the catalog is large, so anything looser would be junk.
/// </summary>
public sealed class CommunityIconFallback(CommunityIconSearch search, CommunityIconImporter importer, ILogger<CommunityIconFallback> logger)
{
    /// <summary>
    /// A community match must score at least this well: one exact, focused hit among a title's ~5 keywords.
    /// Quality comes mostly from the index requiring an exact word and weighting by focus.
    /// </summary>
    internal const double MinimumScore = 0.15;

    // Ties go to Yoto: its icons need no upload.
    private const double MustBeatMargin = 0.01;

    private const int ImportParallelism = 4;

    /// <summary>
    /// Returns an imported icon for each title whose community match beats its score to beat (Yoto's best
    /// score for it, 0 when none), keyed by position in <paramref name="titles"/>. Titles without one are absent.
    /// Never fails because the site is unreachable.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, DisplayIcon>> FindAndImportBetterIconsAsync(
        IReadOnlyList<(string Title, double ScoreToBeat)> titles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titles);

        var picks = await FindBetterMatchesAsync(titles, cancellationToken);
        var imported = new ConcurrentDictionary<string, DisplayIcon>(StringComparer.Ordinal);
        await Parallel.ForEachAsync(
            picks.Values.DistinctBy(icon => icon.Id, StringComparer.Ordinal),
            new ParallelOptions { MaxDegreeOfParallelism = ImportParallelism, CancellationToken = cancellationToken },
            async (icon, token) =>
            {
                if (await TryImportAsync(icon, token) is { } displayIcon)
                {
                    imported[icon.Id] = displayIcon;
                }
            });

        return picks
            .Where(pick => imported.ContainsKey(pick.Value.Id))
            .ToDictionary(pick => pick.Key, pick => imported[pick.Value.Id]);
    }

    private async Task<Dictionary<int, CommunityIcon>> FindBetterMatchesAsync(
        IReadOnlyList<(string Title, double ScoreToBeat)> titles, CancellationToken cancellationToken)
    {
        var picks = new Dictionary<int, CommunityIcon>();
        try
        {
            for (var i = 0; i < titles.Count; i++)
            {
                var minScore = Math.Max(MinimumScore, titles[i].ScoreToBeat + MustBeatMargin);
                if (await search.BestMatchAsync(titles[i].Title, minScore, cancellationToken) is { } icon)
                {
                    picks[i] = icon;
                }
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "yotoicons.com catalog unavailable; matching with Yoto icons only");
            return [];
        }

        return picks;
    }

    private async Task<DisplayIcon?> TryImportAsync(CommunityIcon icon, CancellationToken cancellationToken)
    {
        try
        {
            return await importer.ImportAsync(icon, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not add community icon {IconId}; leaving the track without one", icon.Id);
            return null;
        }
    }
}
