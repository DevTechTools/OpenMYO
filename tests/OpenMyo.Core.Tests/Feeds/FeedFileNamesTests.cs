using OpenMyo.Core.Feeds;

namespace OpenMyo.Core.Tests.Feeds;

public sealed class FeedFileNamesTests
{
    private static FeedEpisode Episode(string url, string? type = null) => new("Title", new Uri(url), null, null, type);

    [Theory]
    [InlineData("https://cdn.example.com/ep.M4A?token=1", null, "001.m4a")]
    [InlineData("https://cdn.example.com/track?id=7", "audio/x-m4a", "001.m4a")]
    [InlineData("https://cdn.example.com/track?id=7", "audio/mpeg; charset=binary", "001.mp3")]
    [InlineData("https://cdn.example.com/track", null, "001.mp3")]
    public void ForEpisode_PicksExtension(string url, string? type, string expected)
    {
        Assert.Equal(expected, FeedFileNames.ForEpisode(Episode(url, type), 0));
    }

    [Fact]
    public void ForEpisode_NumbersInListeningOrder()
    {
        Assert.Equal("012.mp3", FeedFileNames.ForEpisode(Episode("https://cdn.example.com/a.mp3"), 11));
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D }, "cover.png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39 }, "cover.gif")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF }, "cover.jpg")]
    [InlineData(new byte[0], "cover.jpg")]
    public void ForCover_DetectsTypeFromBytes(byte[] header, string expected)
    {
        Assert.Equal(expected, FeedFileNames.ForCover(header));
    }
}
