using OpenMyo.Core.Feeds;

namespace OpenMyo.Core.Tests.Feeds;

public sealed class FeedValuesTests
{
    [Theory]
    [InlineData("Tue, 03 Jun 2025 09:00:00 +0000", "2025-06-03T09:00:00+00:00")]
    [InlineData("Tue, 03 Jun 2025 09:00:00 GMT", "2025-06-03T09:00:00+00:00")]
    [InlineData("Tue, 03 Jun 2025 09:00:00 PDT", "2025-06-03T09:00:00-07:00")]
    [InlineData("Tue, 03 Jun 2025 09:00:00 -0500", "2025-06-03T09:00:00-05:00")]
    [InlineData("2025-06-03T09:00:00Z", "2025-06-03T09:00:00+00:00")]
    public void ParseDate_CommonFeedFormats_AreRead(string text, string expected)
    {
        Assert.Equal(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), FeedValues.ParseDate(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sometime last week")]
    public void ParseDate_Unreadable_ReturnsNull(string? text)
    {
        Assert.Null(FeedValues.ParseDate(text));
    }

    [Theory]
    [InlineData("1:02:03", 3723)]
    [InlineData("12:30", 750)]
    [InlineData("1800", 1800)]
    public void ParseDuration_CommonFormats_AreRead(string text, int seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), FeedValues.ParseDuration(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("about an hour")]
    [InlineData("1:2:3:4")]
    public void ParseDuration_Unreadable_ReturnsNull(string? text)
    {
        Assert.Null(FeedValues.ParseDuration(text));
    }
}
