using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Core.Abstractions;

/// <summary>
/// Keeps the harvested community catalog on disk, so the site is read in full at most about once a week.
/// Both operations are best effort: a missing or unreadable cache is simply not there.
/// </summary>
public interface ICommunityCatalogCache
{
    /// <summary>The last saved catalog, however old, or null when there is none.</summary>
    Task<CommunityCatalog?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(CommunityCatalog catalog, CancellationToken cancellationToken);
}
