namespace OpenMyo.Core.CommunityIcons;

/// <summary>A snapshot of every icon on the community site, harvested at <paramref name="FetchedAt"/>.</summary>
/// <param name="IsComplete">False when some pages could not be read; such a catalog is used but never trusted as fresh.</param>
public sealed record CommunityCatalog(IReadOnlyList<CommunityIcon> Icons, DateTimeOffset FetchedAt, bool IsComplete)
{
    /// <summary>
    /// Fills the gaps of a partial harvest with an older catalog's icons, so a flaky connection never
    /// shrinks what the user can search.
    /// </summary>
    public CommunityCatalog MergedWith(CommunityCatalog older)
    {
        ArgumentNullException.ThrowIfNull(older);
        var icons = Icons.Concat(older.Icons).DistinctBy(icon => icon.Id, StringComparer.Ordinal).ToList();
        return this with { Icons = icons };
    }
}
