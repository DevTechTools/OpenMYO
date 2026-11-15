using Microsoft.Extensions.Logging.Abstractions;
using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Icons;

public sealed class CommunityIconFallbackTests
{
    private static readonly DisplayIcon Owl = TestIcons.Make("owl-id", "owl");

    internal static CommunityIconFallback Fallback(FakeMediaUploader uploader, params CommunityIcon[] icons) =>
        Fallback(uploader, new FakeCommunityCatalogCache(new CommunityCatalog(icons, DateTimeOffset.UtcNow, IsComplete: true)), new FakeCommunityIconSource(0));

    private static CommunityIconFallback Fallback(FakeMediaUploader uploader, FakeCommunityCatalogCache cache, FakeCommunityIconSource source)
    {
        var provider = new CommunityCatalogProvider(new CommunityCatalogHarvester(source), cache);
        var importer = new CommunityIconImporter(source, uploader, new IconIndexProvider(new FakeIconCatalog([])));
        return new CommunityIconFallback(new CommunityIconSearch(provider), importer, NullLogger<CommunityIconFallback>.Instance);
    }

    private static Playlist PlaylistOf(params string[] titles) => new()
    {
        Title = "Stories",
        Chapters = titles.Select(title => new Chapter
        {
            Key = title,
            Title = title,
            Tracks = [new Track { Key = title, Title = title, TrackUrl = "yoto:#x" }],
        }).ToList(),
    };

    private static IconAutoAssigner Assigner(CommunityIconFallback fallback) =>
        new(new FakeIconSuggester(new Dictionary<string, DisplayIcon> { ["Owl"] = Owl }),
            new NumberIconMaker(new FakeMediaUploader(), new IconIndexProvider(new FakeIconCatalog([]))),
            fallback);

    [Fact]
    public async Task MatchIconsAsync_NoYotoMatch_UsesCommunityIcon()
    {
        var uploader = new FakeMediaUploader();
        var assigner = Assigner(Fallback(uploader, FakeCommunityIconSource.Icon("7", "dragon")));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Dragon"), false, false, CancellationToken.None);

        Assert.Equal(1, result.CommunityIconCount);
        Assert.Equal("uploaded-dragon (yotoicons 7)", result.Playlist.Chapters[0].IconMediaId);
        Assert.Equal("uploaded-dragon (yotoicons 7)", result.Playlist.Chapters[0].Tracks[0].IconMediaId);
    }

    [Fact]
    public async Task MatchIconsAsync_YotoMatches_DoesNotUseCommunity()
    {
        var uploader = new FakeMediaUploader();
        var assigner = Assigner(Fallback(uploader, FakeCommunityIconSource.Icon("1", "owl")));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Owl"), false, false, CancellationToken.None);

        Assert.Equal("owl-id", result.Playlist.Chapters[0].IconMediaId);
        Assert.Equal(0, result.CommunityIconCount);
        Assert.Empty(uploader.Icons);
    }

    [Fact]
    public async Task MatchIconsAsync_WeakCommunityMatch_LeavesChapterEmpty()
    {
        var assigner = Assigner(Fallback(new FakeMediaUploader(), FakeCommunityIconSource.Icon("1", "dragonfly")));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Dragon"), false, false, CancellationToken.None);

        Assert.False(result.Playlist.Chapters[0].HasIcon);
    }

    [Fact]
    public async Task MatchIconsAsync_CatalogUnavailable_StillReturnsYotoResults()
    {
        var source = new FakeCommunityIconSource(pageCount: 1);
        source.Failures[1] = int.MaxValue;
        var assigner = Assigner(Fallback(new FakeMediaUploader(), new FakeCommunityCatalogCache(), source));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Owl", "Dragon"), false, false, CancellationToken.None);

        Assert.Equal(["owl-id", null], result.Playlist.Chapters.Select(chapter => chapter.IconMediaId));
        Assert.Equal(0, result.CommunityIconCount);
    }

    [Fact]
    public async Task MatchIconsAsync_OneOfSeveralWordsMatches_UsesCommunityIcon()
    {
        var assigner = Assigner(Fallback(new FakeMediaUploader(), FakeCommunityIconSource.Icon("3", "truth")));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Truth, Trivia and 'Trina"), false, false, CancellationToken.None);

        Assert.Equal(1, result.CommunityIconCount);
    }

    [Fact]
    public async Task MatchIconsAsync_EquallyGoodCommunityIcons_PicksMostDownloaded()
    {
        var assigner = Assigner(Fallback(
            new FakeMediaUploader(), FakeCommunityIconSource.Icon("1", "dragon", downloads: 2), FakeCommunityIconSource.Icon("2", "dragon", downloads: 90)));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Dragon"), false, false, CancellationToken.None);

        Assert.Equal("uploaded-dragon (yotoicons 2)", result.Playlist.Chapters[0].IconMediaId);
    }

    [Fact]
    public async Task MatchIconsAsync_FocusedIconAndLongLyricBothMatch_PicksFocusedIcon()
    {
        var assigner = Assigner(Fallback(
            new FakeMediaUploader(),
            FakeCommunityIconSource.Icon("1", "we didnt start the fire camp rock song lyric", downloads: 500),
            FakeCommunityIconSource.Icon("2", "camp")));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Between Camp and a Hard Place"), false, false, CancellationToken.None);

        Assert.Equal("uploaded-camp (yotoicons 2)", result.Playlist.Chapters[0].IconMediaId);
    }

    [Fact]
    public async Task MatchIconsAsync_SameCommunityIconForTwoTracks_ImportsOnce()
    {
        var uploader = new FakeMediaUploader();
        var assigner = Assigner(Fallback(uploader, FakeCommunityIconSource.Icon("7", "dragon")));

        var result = await assigner.MatchIconsAsync(PlaylistOf("Dragon", "The Dragon"), false, false, CancellationToken.None);

        Assert.Single(uploader.Icons);
        Assert.Equal(2, result.CommunityIconCount);
        Assert.Equal(result.Playlist.Chapters[0].IconMediaId, result.Playlist.Chapters[1].IconMediaId);
    }
}
