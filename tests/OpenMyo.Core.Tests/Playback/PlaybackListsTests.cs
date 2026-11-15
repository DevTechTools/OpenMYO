using OpenMyo.Core.Import;
using OpenMyo.Core.Playback;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Playback;

public sealed class PlaybackListsTests
{
    private static readonly Playlist Sample = new()
    {
        CardId = "abc12",
        Title = "Bedtime",
        Chapters =
        [
            Chapter("01", "One"),
            Chapter("02", "Two"),
            Chapter("03", "Three"),
        ],
    };

    [Fact]
    public void FromPlaylist_StartChapter_StartsThereAndKeepsOrder()
    {
        var queue = PlaybackLists.FromPlaylist(Sample, StreamsFor("01", "02", "03"), "02");

        Assert.NotNull(queue);
        Assert.Equal(["abc12#01", "abc12#02", "abc12#03"], queue.Items.Select(item => item.RowKey));
        Assert.Equal("Two", queue.Current.Title);
    }

    [Fact]
    public void FromPlaylist_TrackWithoutStream_IsSkipped()
    {
        var queue = PlaybackLists.FromPlaylist(Sample, StreamsFor("01", "03"), "03");

        Assert.NotNull(queue);
        Assert.Equal(["abc12#01", "abc12#03"], queue.Items.Select(item => item.RowKey));
        Assert.Equal("abc12#03", queue.Current.RowKey);
    }

    [Fact]
    public void FromPlaylist_NoStreams_ReturnsNull()
    {
        Assert.Null(PlaybackLists.FromPlaylist(Sample, StreamsFor(), "01"));
    }

    [Fact]
    public void FromDraft_LocalFiles_PlayAsFileUris()
    {
        var path = Path.Combine(Path.GetTempPath(), "song.mp3");
        var draft = new PlaylistDraft("Songs", Path.GetTempPath(), [new TrackDraft(path, "Song", TimeSpan.FromMinutes(2))], null);

        var queue = PlaybackLists.FromDraft(draft, 0);

        Assert.True(queue.Current.Source.IsFile);
        Assert.Equal(path, queue.Current.RowKey);
    }

    private static Chapter Chapter(string key, string title) => new()
    {
        Key = key,
        Title = title,
        Tracks = [new Track { Key = "01", Title = title, TrackUrl = "yoto:#" + key }],
    };

    private static Dictionary<TrackAddress, Uri> StreamsFor(params string[] chapterKeys) =>
        chapterKeys.ToDictionary(key => new TrackAddress(key, "01"), key => new Uri($"https://example.com/{key}.mp3"));
}
