using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Library;

/// <summary>A track (chapter row) whose title matched a search.</summary>
/// <param name="Number">1-based position in the playlist, as the user sees it.</param>
public sealed record TrackMatch(string ChapterKey, int Number, string Title, IReadOnlyList<TextMatch> Ranges);

/// <summary>Why a playlist matched a search: its title, any of its tracks, or both.</summary>
public sealed record PlaylistMatch(IReadOnlyList<TextMatch>? TitleRanges, IReadOnlyList<TrackMatch> Tracks)
{
    public bool IsTitleMatch => TitleRanges is not null;
}

/// <summary>Finds a search in a playlist's title and in each of its track titles.</summary>
public static class TrackSearch
{
    /// <summary>
    /// Matches <paramref name="query"/> against the title and, once loaded, every chapter of the playlist.
    /// Returns null when nothing matches. Tracks are chapters because a chapter is one row to the user.
    /// </summary>
    public static PlaylistMatch? Match(SearchQuery query, string title, Playlist? playlist)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(title);

        var titleRanges = query.Match(title);
        var tracks = new List<TrackMatch>();
        var chapters = playlist?.Chapters ?? [];
        for (var i = 0; i < chapters.Count; i++)
        {
            if (query.Match(chapters[i].Title) is { } ranges)
            {
                tracks.Add(new TrackMatch(chapters[i].Key, i + 1, chapters[i].Title, ranges));
            }
        }

        return titleRanges is null && tracks.Count == 0 ? null : new PlaylistMatch(titleRanges, tracks);
    }
}
