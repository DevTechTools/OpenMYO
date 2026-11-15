using OpenMyo.Core.Feeds;

namespace OpenMyo.Core.Tests.Feeds;

public sealed class EpisodeFilterTests
{
    private static FeedEpisode Episode(string title, int? minutes = 20) =>
        new(title, new Uri("https://cdn.example.com/a.mp3"), null, minutes is null ? null : TimeSpan.FromMinutes(minutes.Value), "audio/mpeg");

    private static EpisodeFilter Filter(int? min = null, int? max = null, string exclude = "", string include = "") => new(
        min is null ? null : TimeSpan.FromMinutes(min.Value),
        max is null ? null : TimeSpan.FromMinutes(max.Value),
        EpisodeFilter.ParseWords(exclude),
        EpisodeFilter.ParseWords(include));

    [Fact]
    public void Matches_NoConditions_KeepsEverything()
    {
        Assert.True(EpisodeFilter.None.Matches(Episode("Anything", 500)));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void Matches_MaxLength_DropsLongerEpisodes(int minutes, bool expected)
    {
        Assert.Equal(expected, Filter(max: 30).Matches(Episode("Story", minutes)));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(5, true)]
    public void Matches_MinLength_DropsShorterEpisodes(int minutes, bool expected)
    {
        Assert.Equal(expected, Filter(min: 5).Matches(Episode("Story", minutes)));
    }

    [Fact]
    public void Matches_UnknownLength_PassesLengthLimits()
    {
        Assert.True(Filter(min: 5, max: 30).Matches(Episode("Story", minutes: null)));
    }

    [Theory]
    [InlineData("Season 2 TRAILER", false)]
    [InlineData("Bonus: behind the scenes", false)]
    [InlineData("The Owl", true)]
    public void Matches_ExcludeWords_DropsTitlesContainingAny(string title, bool expected)
    {
        Assert.Equal(expected, Filter(exclude: "trailer, bonus").Matches(Episode(title)));
    }

    [Theory]
    [InlineData("Bedtime Story: The Owl", true)]
    [InlineData("News roundup", false)]
    public void Matches_IncludeWords_KeepsOnlyTitlesContainingOne(string title, bool expected)
    {
        Assert.Equal(expected, Filter(include: "story,tale").Matches(Episode(title)));
    }

    [Fact]
    public void Matches_ExcludeBeatsInclude()
    {
        Assert.False(Filter(exclude: "trailer", include: "story").Matches(Episode("Story trailer")));
    }

    [Fact]
    public void ParseWords_TrimsAndDropsBlanks()
    {
        Assert.Equal(["trailer", "bonus"], EpisodeFilter.ParseWords(" trailer, ,bonus ,"));
    }
}
