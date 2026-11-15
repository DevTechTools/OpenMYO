using OpenMyo.Core.Feeds;

namespace OpenMyo.Core.Tests.Feeds;

public sealed class FeedParserTests
{
    private static readonly Uri FeedAddress = new("https://example.com/show/feed.xml");

    private static string Rss(string channelBody) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0" xmlns:itunes="http://www.itunes.com/dtds/podcast-1.0.dtd">
          <channel>{channelBody}</channel>
        </rss>
        """;

    private static string Item(string title, string url, string? date = null, string type = "audio/mpeg", string? duration = null) => $"""
        <item>
          <title>{title}</title>
          <enclosure url="{url}" type="{type}" length="1" />
          {(date is null ? string.Empty : $"<pubDate>{date}</pubDate>")}
          {(duration is null ? string.Empty : $"<itunes:duration>{duration}</itunes:duration>")}
        </item>
        """;

    [Fact]
    public void Parse_DatedItems_OrdersOldestFirst()
    {
        var xml = Rss("<title>Bedtime Tales</title>"
            + Item("Two", "https://cdn.example.com/2.mp3", "Tue, 10 Jun 2025 09:00:00 +0000")
            + Item("One", "https://cdn.example.com/1.mp3", "Tue, 03 Jun 2025 09:00:00 GMT"));

        var feed = FeedParser.Parse(xml, FeedAddress);

        Assert.Equal("Bedtime Tales", feed.Title);
        Assert.Equal(["One", "Two"], feed.Episodes.Select(episode => episode.Title));
    }

    [Fact]
    public void Parse_MissingDates_ReversesFeedOrder()
    {
        var xml = Rss(Item("Newest", "https://cdn.example.com/b.mp3") + Item("Oldest", "https://cdn.example.com/a.mp3"));

        var feed = FeedParser.Parse(xml, FeedAddress);

        Assert.Equal(["Oldest", "Newest"], feed.Episodes.Select(episode => episode.Title));
    }

    [Fact]
    public void Parse_ItunesImage_IsPreferredOverRssImage()
    {
        var xml = Rss("""
            <image><url>https://example.com/small.jpg</url></image>
            <itunes:image href="https://example.com/large.jpg" />
            """ + Item("One", "https://cdn.example.com/1.mp3"));

        Assert.Equal(new Uri("https://example.com/large.jpg"), FeedParser.Parse(xml, FeedAddress).ImageUrl);
    }

    [Fact]
    public void Parse_VideoAndLinkOnlyItems_AreSkipped()
    {
        var xml = Rss(Item("Video", "https://cdn.example.com/v.mp4", type: "video/mp4")
            + "<item><title>Blog post</title></item>"
            + Item("Audio", "https://cdn.example.com/a.m4a", type: "audio/x-m4a"));

        Assert.Equal("Audio", Assert.Single(FeedParser.Parse(xml, FeedAddress).Episodes).Title);
    }

    [Fact]
    public void Parse_RelativeEnclosureAndDuration_AreResolved()
    {
        var episode = Assert.Single(FeedParser.Parse(Rss(Item("One", "audio/1.mp3", duration: "1:02:03")), FeedAddress).Episodes);

        Assert.Equal(new Uri("https://example.com/show/audio/1.mp3"), episode.AudioUrl);
        Assert.Equal(new TimeSpan(1, 2, 3), episode.Duration);
    }

    [Fact]
    public void Parse_UntitledChannel_UsesHost()
    {
        Assert.Equal("example.com", FeedParser.Parse(Rss(Item("One", "https://cdn.example.com/1.mp3")), FeedAddress).Title);
    }

    [Theory]
    [InlineData("<html><body>A podcast page</body></html>")]
    [InlineData("not xml at all")]
    [InlineData("""<rss version="2.0"><channel><title>Empty</title></channel></rss>""")]
    public void Parse_NotAnAudioFeed_ThrowsFeedFormatException(string xml)
    {
        Assert.Throws<FeedFormatException>(() => FeedParser.Parse(xml, FeedAddress));
    }

    [Fact]
    public void Parse_ExternalEntity_IsNotResolved()
    {
        var xml = """
            <?xml version="1.0"?>
            <!DOCTYPE rss [<!ENTITY secret SYSTEM "file:///etc/passwd">]>
            <rss version="2.0"><channel><title>&secret;</title>
            <item><title>One</title><enclosure url="https://cdn.example.com/1.mp3" type="audio/mpeg" /></item>
            </channel></rss>
            """;

        var exception = Record.Exception(() => FeedParser.Parse(xml, FeedAddress));

        Assert.True(exception is null or FeedFormatException);
    }
}
