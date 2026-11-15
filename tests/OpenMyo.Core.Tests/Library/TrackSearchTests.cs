using OpenMyo.Core.Library;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Library;

public class TrackSearchTests
{
    private static Playlist MakePlaylist(params string[] chapterTitles) => new()
    {
        Title = "Bedtime",
        Chapters = chapterTitles
            .Select((title, i) => new Chapter { Key = $"{i + 1:00}", Title = title })
            .ToList(),
    };

    [Theory]
    [InlineData("gruff child", "The Gruffalo's Child", true)]
    [InlineData("GRUFFALO", "The Gruffalo's Child", true)]
    [InlineData("ele", "Élé the elephant", true)]
    [InlineData("child gruff", "The Gruffalo's Child", true)]
    [InlineData("gruff mouse", "The Gruffalo's Child", false)]
    public void Match_Words_RequiresEveryWordIgnoringCaseAndAccents(string query, string text, bool expected)
    {
        Assert.Equal(expected, SearchQuery.Parse(query).Match(text) is not null);
    }

    [Fact]
    public void Match_OverlappingWords_MergesRanges()
    {
        var ranges = SearchQuery.Parse("gruff ruffalo").Match("The Gruffalo");

        Assert.Equal([new TextMatch(4, 8)], ranges);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_Blank_IsEmpty(string? text)
    {
        Assert.True(SearchQuery.Parse(text).IsEmpty);
    }

    [Fact]
    public void Match_TrackTitle_ReturnsTrackWithNumberAndKey()
    {
        var playlist = MakePlaylist("Intro", "The Gruffalo's Child", "Outro");

        var match = TrackSearch.Match(SearchQuery.Parse("gruffalo"), playlist.Title, playlist);

        Assert.NotNull(match);
        Assert.False(match.IsTitleMatch);
        var track = Assert.Single(match.Tracks);
        Assert.Equal(("02", 2, "The Gruffalo's Child"), (track.ChapterKey, track.Number, track.Title));
    }

    [Fact]
    public void Match_PlaylistTitleOnly_IsTitleMatchWithoutTracks()
    {
        var playlist = MakePlaylist("Intro");

        var match = TrackSearch.Match(SearchQuery.Parse("bed"), playlist.Title, playlist);

        Assert.NotNull(match);
        Assert.True(match.IsTitleMatch);
        Assert.Empty(match.Tracks);
    }

    [Fact]
    public void Match_NotLoadedYet_MatchesTitleOnly()
    {
        Assert.Null(TrackSearch.Match(SearchQuery.Parse("gruffalo"), "Bedtime", playlist: null));
    }

    [Fact]
    public void Split_Ranges_AlternatesPlainAndMatch()
    {
        var segments = TextSegment.Split("The Gruffalo's Child", [new TextMatch(4, 5), new TextMatch(15, 5)]);

        Assert.Equal(
            [new("The ", false), new("Gruff", true), new("alo's ", false), new("Child", true)],
            segments);
    }

    [Fact]
    public void Split_NoRanges_ReturnsWholeTextPlain()
    {
        Assert.Equal([new TextSegment("Intro", false)], TextSegment.Split("Intro", null));
    }

    [Fact]
    public void Match_NothingMatches_ReturnsNull()
    {
        var playlist = MakePlaylist("Intro");

        Assert.Null(TrackSearch.Match(SearchQuery.Parse("zebra"), playlist.Title, playlist));
    }
}
