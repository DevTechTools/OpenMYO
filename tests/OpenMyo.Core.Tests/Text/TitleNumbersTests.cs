using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class TitleNumbersTests
{
    [Theory]
    [InlineData("[052a] Bonus!", 52)]
    [InlineData("Episode 7", 7)]
    [InlineData("Intro", null)]
    public void LeadingNumber_ReturnsFirstNumber(string title, int? expected) => Assert.Equal(expected, TitleNumbers.LeadingNumber(title));

    [Fact]
    public void IsOutOfOrder_BonusEpisodesAfterTheirEpisode_IsInOrder()
    {
        Assert.False(TitleNumbers.IsOutOfOrder(["[101] Scattered Seeds", "[101a] Bonus! Bloopers", "[102] The Treasure"]));
    }

    [Fact]
    public void IsOutOfOrder_SwappedEpisodes_IsOutOfOrder()
    {
        Assert.True(TitleNumbers.IsOutOfOrder(["[102] Two", "[101] One"]));
    }

    [Fact]
    public void SortByNumber_KeepsOrderAmongEqualNumbers()
    {
        var sorted = TitleNumbers.SortByNumber(["[102] Two", "[101] One", "[101a] Bonus"], title => title);

        Assert.Equal(["[101] One", "[101a] Bonus", "[102] Two"], sorted);
    }

    [Fact]
    public void SuitedTo_BracketNumberedPlaylist_KeepsBrackets()
    {
        var review = TitleReview.Evaluate(["[101] Scattered Seeds", "[101a] Bonus! Bloopers", "[102] The Treasure"]);

        Assert.Equal(0, review.TidyCount);
        Assert.False(review.HasSuggestions);
    }
}
