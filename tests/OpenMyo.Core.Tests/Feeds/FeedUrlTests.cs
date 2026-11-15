using OpenMyo.Core.Feeds;

namespace OpenMyo.Core.Tests.Feeds;

public sealed class FeedUrlTests
{
    [Theory]
    [InlineData("https://example.com/feed.xml", "https://example.com/feed.xml")]
    [InlineData("  http://example.com/rss  ", "http://example.com/rss")]
    [InlineData("example.com/feed.xml", "https://example.com/feed.xml")]
    [InlineData("feed://example.com/rss", "https://example.com/rss")]
    [InlineData("itpc://example.com/rss", "https://example.com/rss")]
    [InlineData("feed://https://example.com/rss", "https://example.com/rss")]
    public void TryParse_WebOrPodcastLink_ReturnsHttpAddress(string text, string expected)
    {
        Assert.Equal(new Uri(expected), FeedUrl.TryParse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello world")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/feed.xml")]
    [InlineData("localhost")]
    public void TryParse_NotAWebAddress_ReturnsNull(string? text)
    {
        Assert.Null(FeedUrl.TryParse(text));
    }
}
