using OpenMyo.Core.Import;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Playback;

/// <summary>Builds play orders from a playlist on Yoto or from files about to be imported.</summary>
public static class PlaybackLists
{
    /// <summary>
    /// Every track of <paramref name="playlist"/> in its current (possibly unsaved) order, starting at the first
    /// track of <paramref name="startChapterKey"/>. Tracks Yoto gave no stream for are skipped.
    /// </summary>
    /// <returns>The list, or null when nothing in the playlist can be played.</returns>
    public static PlaybackList? FromPlaylist(
        Playlist playlist, IReadOnlyDictionary<TrackAddress, Uri> streams, string startChapterKey)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        ArgumentNullException.ThrowIfNull(streams);
        var items = new List<PlaybackItem>();
        var start = -1;
        foreach (var chapter in playlist.Chapters)
        {
            if (start < 0 && string.Equals(chapter.Key, startChapterKey, StringComparison.Ordinal))
            {
                start = items.Count;
            }

            items.AddRange(chapter.Tracks
                .Where(track => streams.ContainsKey(new TrackAddress(chapter.Key, track.Key)))
                .Select(track => new PlaybackItem(
                    RowKey(playlist.CardId, chapter.Key), TitleOf(chapter, track), streams[new TrackAddress(chapter.Key, track.Key)], track.Duration)));
        }

        return items.Count == 0 ? null : new PlaybackList(items, Math.Clamp(start, 0, items.Count - 1));
    }

    /// <summary>The draft's local files in their current order, starting at <paramref name="startIndex"/>.</summary>
    public static PlaybackList FromDraft(PlaylistDraft draft, int startIndex)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var items = draft.Tracks
            .Select(track => new PlaybackItem(track.SourcePath, track.Title, new Uri(Path.GetFullPath(track.SourcePath)), track.Duration))
            .ToList();
        return new PlaybackList(items, startIndex);
    }

    /// <summary>
    /// Identifies a playlist row across playlists: chapter keys ("01") repeat from card to card, so the card id is
    /// part of it. Rows use this to tell whether they are the one playing.
    /// </summary>
    public static string RowKey(string? cardId, string chapterKey) => $"{cardId}#{chapterKey}";

    /// <summary>The chapter key inside a <see cref="RowKey"/>, or null for a row that is a local file.</summary>
    public static string? ChapterKeyOf(string rowKey)
    {
        ArgumentNullException.ThrowIfNull(rowKey);
        var separator = rowKey.LastIndexOf('#');
        return separator < 0 ? null : rowKey[(separator + 1)..];
    }

    // A single-track chapter is one row to the user, so the row's title is the one to show.
    private static string TitleOf(Chapter chapter, Track track) =>
        chapter.Tracks.Count == 1 || string.IsNullOrWhiteSpace(track.Title) ? chapter.Title : track.Title;
}
