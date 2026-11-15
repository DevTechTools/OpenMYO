using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Icons;

public class IconAutoAssignerTests
{
    private static readonly DisplayIcon Owl = TestIcons.Make("owl-id", "owl");
    private static readonly DisplayIcon Moon = TestIcons.Make("moon-id", "moon");

    private static Chapter MakeChapter(string title, string? iconId = null) => new()
    {
        Key = title,
        Title = title,
        IconMediaId = iconId,
        Tracks = [new Track { Key = title, Title = title, TrackUrl = "yoto:#x", IconMediaId = iconId }],
    };

    private static NumberIconMaker Maker(FakeMediaUploader? uploader = null, params DisplayIcon[] icons) =>
        new(uploader ?? new FakeMediaUploader(), new IconIndexProvider(new FakeIconCatalog(icons)));

    private static CommunityIconFallback NoCommunity() => CommunityIconFallbackTests.Fallback(new FakeMediaUploader());

    private static FakeIconSuggester Suggester() =>
        new(new Dictionary<string, DisplayIcon> { ["Owl"] = Owl, ["Moon"] = Moon });

    [Fact]
    public async Task MatchIconsAsync_ReplaceExistingFalse_OnlyFillsMissingIcons()
    {
        var playlist = new Playlist { Title = "Night", Chapters = [MakeChapter("Owl", "keep-me"), MakeChapter("Moon")] };
        var suggester = Suggester();

        var result = (await new IconAutoAssigner(suggester, Maker(), NoCommunity()).MatchIconsAsync(playlist, replaceExisting: false, useAi: false, CancellationToken.None)).Playlist;

        Assert.Equal(["keep-me", "moon-id"], result.Chapters.Select(chapter => chapter.IconMediaId));
        Assert.Equal(["Moon"], suggester.LastRequest!.TrackTitles);
    }

    [Fact]
    public async Task MatchIconsAsync_ReplaceExistingTrue_OverwritesExistingIcons()
    {
        var playlist = new Playlist { Title = "Night", Chapters = [MakeChapter("Owl", "old"), MakeChapter("Moon")] };

        var result = (await new IconAutoAssigner(Suggester(), Maker(), NoCommunity()).MatchIconsAsync(playlist, replaceExisting: true, useAi: false, CancellationToken.None)).Playlist;

        Assert.Equal(["owl-id", "moon-id"], result.Chapters.Select(chapter => chapter.IconMediaId));
    }

    [Fact]
    public async Task MatchIconsAsync_Match_SetsIconOnTracksToo()
    {
        var playlist = new Playlist { Title = "Night", Chapters = [MakeChapter("Owl")] };

        var result = (await new IconAutoAssigner(Suggester(), Maker(), NoCommunity()).MatchIconsAsync(playlist, replaceExisting: false, useAi: false, CancellationToken.None)).Playlist;

        Assert.Equal("owl-id", Assert.Single(result.Chapters[0].Tracks).IconMediaId);
    }

    [Fact]
    public async Task MatchIconsAsync_NoSuggestion_LeavesChapterUnchanged()
    {
        var playlist = new Playlist { Title = "Night", Chapters = [MakeChapter("Unknown")] };

        var result = (await new IconAutoAssigner(Suggester(), Maker(), NoCommunity()).MatchIconsAsync(playlist, replaceExisting: false, useAi: false, CancellationToken.None)).Playlist;

        Assert.False(result.Chapters[0].HasIcon);
    }

    [Fact]
    public async Task MatchIconsAsync_NothingToAssign_ReturnsSamePlaylistWithoutSuggesting()
    {
        var playlist = new Playlist { Title = "Night", Chapters = [MakeChapter("Owl", "have")] };
        var suggester = Suggester();

        var result = (await new IconAutoAssigner(suggester, Maker(), NoCommunity()).MatchIconsAsync(playlist, replaceExisting: false, useAi: false, CancellationToken.None)).Playlist;

        Assert.Same(playlist, result);
        Assert.Null(suggester.LastRequest);
    }

    [Fact]
    public void WithIcon_NullMediaId_ClearsChapterAndTrackIcons()
    {
        var result = IconAutoAssigner.WithIcon(MakeChapter("Owl", "old"), null);

        Assert.False(result.HasIcon);
        Assert.Null(result.Tracks[0].IconMediaId);
    }

    [Fact]
    public async Task AssignNumberIconsAsync_UsesYotoNumberIcon_AndDrawsMissingOnes()
    {
        var one = new DisplayIcon("one-id", "Number - 1", [], null, IsPublic: true);
        var playlist = new Playlist { Title = "Night", Chapters = [MakeChapter("Owl"), MakeChapter("Moon")] };

        var uploader = new FakeMediaUploader();

        var result = await new IconAutoAssigner(Suggester(), Maker(uploader, one), NoCommunity()).AssignNumberIconsAsync(playlist, [0, 1], CancellationToken.None);

        Assert.Equal(["one-id", "uploaded-OpenMyo number 2"], result.Playlist.Chapters.Select(chapter => chapter.IconMediaId));
        Assert.Empty(result.MissingNumbers);
        Assert.Equal("image/png", Assert.Single(uploader.Icons).ContentType);
    }
}
