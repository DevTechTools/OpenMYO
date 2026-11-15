using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Mcp.Support;

/// <summary>Names icons for results by looking their ids up in the shared icon index.</summary>
public sealed class IconLabeler(IconIndexProvider indexProvider)
{
    /// <summary>The icon's id and title; null when <paramref name="mediaId"/> is empty.</summary>
    public async Task<IconRef?> DescribeIconAsync(string? mediaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(mediaId))
        {
            return null;
        }

        var index = await indexProvider.GetAsync(cancellationToken);
        return ToRef(index, mediaId);
    }

    /// <summary>The icons the 1-based <paramref name="tracks"/> have now, so a change can be undone.</summary>
    public async Task<IReadOnlyList<PreviousIcon>> GetCurrentIconsAsync(
        Playlist playlist, IEnumerable<int> tracks, CancellationToken cancellationToken)
    {
        var index = await indexProvider.GetAsync(cancellationToken);
        return tracks.Select(track => new PreviousIcon(track, CurrentIconId(playlist, track), IconTitle(index, playlist, track))).ToList();
    }

    private static string? CurrentIconId(Playlist playlist, int track) => playlist.Chapters[track - 1].IconMediaId;

    private static string? IconTitle(IconIndex index, Playlist playlist, int track) =>
        CurrentIconId(playlist, track) is { Length: > 0 } id ? index.Find(id)?.Title : null;

    private static IconRef ToRef(IconIndex index, string mediaId)
    {
        var title = index.Find(mediaId)?.Title;
        return new IconRef(mediaId, string.IsNullOrEmpty(title) ? null : title);
    }
}
