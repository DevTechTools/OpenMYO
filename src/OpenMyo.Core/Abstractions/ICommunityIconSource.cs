using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Core.Abstractions;

/// <summary>
/// A community icon site (yotoicons.com). Its listing is read page by page to build a local catalog, so
/// searching never waits on the site; images are downloaded one at a time, only when shown or chosen.
/// </summary>
public interface ICommunityIconSource
{
    /// <summary>Reads one listing page (1-based), newest icons first.</summary>
    /// <exception cref="HttpRequestException">The site could not be reached or timed out.</exception>
    Task<CommunityCatalogPage> GetPageAsync(int page, CancellationToken cancellationToken);

    /// <summary>Downloads the icon's PNG so it can be uploaded to the user's own Yoto icons.</summary>
    Task<ReadOnlyMemory<byte>> DownloadAsync(CommunityIcon icon, CancellationToken cancellationToken);
}
