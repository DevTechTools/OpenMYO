using OpenMyo.Core.Playlists;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Turns the icons already used in the user's playlists into a searchable source. Many of them are not in
/// Yoto's public catalog (the user's uploads, icons shared by other creators); the titles of the tracks they
/// were used on stand in for tags, so "connie" finds the icon already used for Connie's episodes.
/// </summary>
public static class LibraryIcons
{
    private const int MaxTagsPerIcon = 12;

    public static IReadOnlyList<DisplayIcon> FromPlaylists(IEnumerable<Playlist> playlists)
    {
        ArgumentNullException.ThrowIfNull(playlists);

        return playlists
            .SelectMany(playlist => playlist.Chapters)
            .Where(chapter => chapter.HasIcon)
            .GroupBy(chapter => chapter.IconMediaId ?? string.Empty, StringComparer.Ordinal)
            .Select(group => ToIcon(group.Key, group.Select(chapter => chapter.Title).ToList()))
            .ToList();
    }

    private static DisplayIcon ToIcon(string mediaId, List<string> titles)
    {
        var tags = titles
            .SelectMany(TextNormalizer.Keywords)
            .GroupBy(word => word, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .Take(MaxTagsPerIcon)
            .ToList();

        // Image URL is resolved by the app from the media id; Core does not know Yoto's hosts.
        return new DisplayIcon(mediaId, titles[0], tags, ImageUrl: null, IsPublic: false);
    }
}
