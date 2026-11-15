using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Players;

/// <summary>Turns a row in a playlist into something a Yoto player can start, and finds the rows either side.</summary>
public static class PlayTargets
{
    /// <summary>
    /// Starts at the first track of the chapter with <paramref name="chapterKey"/>. Returns null when the playlist
    /// is not saved on Yoto yet, or the chapter has no tracks, because a player can only play saved cards.
    /// </summary>
    public static PlayTarget? ForChapter(Playlist playlist, string chapterKey, TimeSpan startAt)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        var chapter = Find(playlist, chapterKey);
        return playlist.CardId is { } cardId && chapter is { Tracks: [var first, ..] }
            ? new PlayTarget(cardId, chapter.Key, first.Key, startAt)
            : null;
    }

    /// <summary>The chapter <paramref name="offset"/> rows from <paramref name="chapterKey"/>, or null past either end.</summary>
    public static string? Neighbour(Playlist playlist, string chapterKey, int offset)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        var chapters = playlist.Chapters;
        for (var i = 0; i < chapters.Count; i++)
        {
            if (string.Equals(chapters[i].Key, chapterKey, StringComparison.Ordinal))
            {
                var target = i + offset;
                return target >= 0 && target < chapters.Count ? chapters[target].Key : null;
            }
        }

        return null;
    }

    private static Chapter? Find(Playlist playlist, string chapterKey) =>
        playlist.Chapters.FirstOrDefault(chapter => string.Equals(chapter.Key, chapterKey, StringComparison.Ordinal));
}
