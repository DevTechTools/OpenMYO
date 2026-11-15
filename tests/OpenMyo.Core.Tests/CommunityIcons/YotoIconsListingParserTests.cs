using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Core.Tests.CommunityIcons;

public sealed class YotoIconsListingParserTests
{
    private const string Listing = """
        <div class="icon" onclick="populate_icon_modal('578', 'animals', 'dragon', 'fire', 'pangolinpaw', '2890');">
        <div class="icon" onclick="populate_icon_modal('99', 'people', 'King\'s Crown', '', '', '12');">
        <div class="icon" onclick="populate_icon_modal('' + id + '', 'x', 'y', 'z', 'a', '1');">
        """;

    [Fact]
    public void Parse_ListingPage_ReadsIdTagsArtistAndImage()
    {
        var icons = YotoIconsListingParser.Parse(Listing);

        Assert.Equal(2, icons.Count);
        var dragon = icons[0];
        Assert.Equal("578", dragon.Id);
        Assert.Equal("dragon · fire", dragon.Title);
        Assert.Equal(["dragon", "fire", "animals"], dragon.Tags);
        Assert.Equal("pangolinpaw", dragon.Artist);
        Assert.Equal(2890, dragon.Downloads);
        Assert.Equal("https://yotoicons.com/static/uploads/578.png", dragon.ImageUrl.AbsoluteUri);
    }

    [Fact]
    public void Parse_EscapedQuoteAndUnknownArtist_AreHandled()
    {
        var king = YotoIconsListingParser.Parse(Listing)[1];

        Assert.Equal("King's Crown", king.Title);
        Assert.Null(king.Artist);
        Assert.Equal("yotoicons.com", king.Credit);
    }

    [Theory]
    [InlineData("<script>var pages = Math.ceil(26199 / 25);</script>", 26199)]
    [InlineData("<script>Math.ceil( 7 /25 )</script>", 7)]
    [InlineData("<p>No pager here</p>", null)]
    public void TotalCount_PagerScript_ReadsTotal(string html, int? expected)
    {
        Assert.Equal(expected, YotoIconsListingParser.TotalCount(html));
    }
}
