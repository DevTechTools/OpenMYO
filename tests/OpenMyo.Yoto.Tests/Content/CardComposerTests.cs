using OpenMyo.Core.Playlists;
using OpenMyo.Core.Text;
using OpenMyo.Yoto.Content;

namespace OpenMyo.Yoto.Tests.Content;

public sealed class CardComposerTests
{
    [Fact]
    public void Compose_UnchangedPlaylist_PreservesUnknownFieldsAtEveryLevel()
    {
        var raw = Samples.Card();
        var playlist = CardMapper.ToPlaylist(raw);

        var body = CardComposer.Compose(raw, playlist);

        Assert.True((bool)body["extraCardField"]!["keep"]!);
        Assert.Equal(2592000, (int)body["content"]!["config"]!["resumeTimeout"]!);
        Assert.Equal(7, (int)body["content"]!["chapters"]![0]!["chapterOnly"]!);
        Assert.Equal("x", (string)body["content"]!["chapters"]![0]!["tracks"]![0]!["trackOnly"]!);
        Assert.Equal(1, (int)body["metadata"]!["other"]!);
    }

    [Fact]
    public void Compose_RawCard_DropsServerManagedFields()
    {
        var raw = Samples.Card();

        var body = CardComposer.Compose(raw, CardMapper.ToPlaylist(raw));

        Assert.Null(body["createdAt"]);
        Assert.Null(body["updatedAt"]);
        Assert.Null(body["userId"]);
        Assert.Equal("abc12", (string)body["cardId"]!);
        Assert.NotNull(raw["createdAt"]);
    }

    [Fact]
    public void Compose_ReorderedChapters_RenumbersKeysButKeepsOwnFields()
    {
        var raw = Samples.Card();
        var playlist = CardMapper.ToPlaylist(raw);
        playlist = playlist with { Chapters = [playlist.Chapters[1], playlist.Chapters[0]] };

        var chapters = CardComposer.Compose(raw, playlist)["content"]!["chapters"]!;

        Assert.Equal("01", (string)chapters[0]!["key"]!);
        Assert.Equal("Two", (string)chapters[0]!["title"]!);
        Assert.Equal("1", (string)chapters[0]!["overlayLabel"]!);
        Assert.Equal("02", (string)chapters[1]!["key"]!);
        Assert.Equal(7, (int)chapters[1]!["chapterOnly"]!);
        Assert.Equal("2", (string)chapters[1]!["overlayLabel"]!);
    }

    [Fact]
    public void Compose_SeveralChapters_NumbersTrackLabelsAcrossTheCardAndKeysPerChapter()
    {
        var playlist = new Playlist
        {
            Title = "Stories",
            Chapters =
            [
                LabelChapter("a", "a1", "a2"),
                LabelChapter("b", "b1"),
                LabelChapter("c", "c1"),
            ],
        };

        var chapters = CardComposer.Compose(null, playlist)["content"]!["chapters"]!.AsArray();
        var tracks = chapters.SelectMany(chapter => chapter!["tracks"]!.AsArray()).ToList();

        Assert.Equal(["1", "2", "3", "4"], tracks.Select(track => (string)track!["overlayLabel"]!));
        Assert.Equal(["01", "02", "01", "01"], tracks.Select(track => (string)track!["key"]!));
        Assert.Equal(["1", "2", "3"], chapters.Select(chapter => (string)chapter!["overlayLabel"]!));
    }

    private static Chapter LabelChapter(string key, params string[] trackKeys) => new()
    {
        Key = key,
        Title = key,
        Tracks = trackKeys.Select(trackKey => new Track { Key = trackKey, Title = trackKey, TrackUrl = "yoto:#" + trackKey }).ToList(),
    };

    [Fact]
    public void Compose_InvalidIconId_OmitsDisplayIcon()
    {
        var playlist = new Playlist
        {
            Title = "New",
            Chapters = [new Chapter { Key = "n", Title = "C", IconMediaId = "short", Tracks = [NewTrack("t")] }],
        };

        var chapter = CardComposer.Compose(null, playlist)["content"]!["chapters"]![0]!;

        Assert.Null(chapter["display"]);
        Assert.Null(chapter["tracks"]![0]!["display"]);
    }

    [Fact]
    public void Compose_ClearedIcon_RemovesExistingIconButKeepsOtherDisplayFields()
    {
        var raw = Samples.Card();
        raw["content"]!["chapters"]![1]!["display"]!["other"] = "kept";
        var playlist = CardMapper.ToPlaylist(raw);
        playlist = playlist with { Chapters = [playlist.Chapters[0], playlist.Chapters[1] with { IconMediaId = null }] };

        var display = CardComposer.Compose(raw, playlist)["content"]!["chapters"]![1]!["display"]!;

        Assert.Null(display["icon16x16"]);
        Assert.Equal("kept", (string)display["other"]!);
    }

    [Fact]
    public void Compose_NewPlaylistWithCoverAndIcon_SetsMetadataAndIcons()
    {
        var playlist = new Playlist
        {
            Title = "  New  ",
            CoverImageUrl = new Uri("https://example.com/cover.png"),
            Chapters = [new Chapter { Key = "n", Title = "C", IconMediaId = Samples.IconA, Tracks = [NewTrack("t")] }],
        };

        var body = CardComposer.Compose(null, playlist);

        Assert.Equal("New", (string)body["title"]!);
        Assert.Null(body["cardId"]);
        Assert.Equal("https://example.com/cover.png", (string)body["metadata"]!["cover"]!["imageL"]!);
        Assert.Equal(60, (double)body["metadata"]!["media"]!["duration"]!);
        Assert.Equal(500, (long)body["metadata"]!["media"]!["fileSize"]!);
        Assert.Equal("yoto:#" + Samples.IconA, (string)body["content"]!["chapters"]![0]!["display"]!["icon16x16"]!);
        Assert.Equal("audio", (string)body["content"]!["chapters"]![0]!["tracks"]![0]!["type"]!);
    }

    [Fact]
    public void Compose_EmptyTitle_Throws() =>
        Assert.Throws<ArgumentException>(() => CardComposer.Compose(null, new Playlist { Title = "  " }));

    [Fact]
    public void Compose_ChapterTitleOverYotoLimit_ThrowsNamingTheTrack()
    {
        var longTitle = new string('a', TitleCleaner.MaxTitleLength + 1);
        var playlist = new Playlist
        {
            Title = "New",
            Chapters = [new Chapter { Key = "a", Title = "Fine", Tracks = [NewTrack("t")] }, new Chapter { Key = "b", Title = longTitle, Tracks = [NewTrack("u")] }],
        };

        var error = Assert.Throws<ArgumentException>(() => CardComposer.Compose(null, playlist));

        Assert.Contains("Track(s) 2 ", error.Message, StringComparison.Ordinal);
    }

    private static Track NewTrack(string key) => new()
    {
        Key = key,
        Title = "T",
        TrackUrl = "yoto:#sha",
        Duration = TimeSpan.FromSeconds(60),
        FileSize = 500,
        Channels = "stereo",
    };
}
