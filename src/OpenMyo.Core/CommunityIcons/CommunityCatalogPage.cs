namespace OpenMyo.Core.CommunityIcons;

/// <summary>One listing page of the community site.</summary>
/// <param name="Icons">The page's icons; empty past the last page.</param>
/// <param name="TotalCount">How many icons the whole listing has, when the page says; tells the harvester how many pages to read.</param>
public sealed record CommunityCatalogPage(IReadOnlyList<CommunityIcon> Icons, int? TotalCount);
