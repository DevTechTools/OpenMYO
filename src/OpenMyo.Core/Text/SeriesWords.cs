namespace OpenMyo.Core.Text;

/// <summary>
/// Finds words a playlist repeats in most of its titles ("B-TV", a show's name). They name the series, not
/// the track, so they should not decide each track's icon.
/// </summary>
public static class SeriesWords
{
    private const int MinimumTitles = 4;
    private const double MinimumShare = 0.5;

    public static IReadOnlySet<string> Find(IReadOnlyList<string> titles)
    {
        ArgumentNullException.ThrowIfNull(titles);

        if (titles.Count < MinimumTitles)
        {
            return new HashSet<string>();
        }

        return titles
            .SelectMany(title => TextNormalizer.Keywords(IconQuery.Clean(title)))
            .CountBy(word => word)
            .Where(pair => pair.Value >= titles.Count * MinimumShare)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
    }
}
