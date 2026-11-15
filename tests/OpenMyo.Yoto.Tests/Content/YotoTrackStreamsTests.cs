using OpenMyo.Core.Playback;
using OpenMyo.Yoto.Content;
using OpenMyo.Yoto.Tests.Support;

namespace OpenMyo.Yoto.Tests.Content;

public sealed class YotoTrackStreamsTests
{
    private const string Route = "GET /content/abc12?playable=true&signingType=s3";

    [Fact]
    public async Task GetStreamsAsync_SignedCard_MapsEachTrackByChapterAndTrackKey()
    {
        var card = Samples.Card();
        card["content"]!["chapters"]![0]!["tracks"]![0]!["trackUrl"] = "https://secure-media.example.com/one.mp3?sig=1";
        card["content"]!["chapters"]![1]!["tracks"]![0]!["trackUrl"] = "https://secure-media.example.com/two.aac?sig=2";
        var handler = new StubHttpHandler().On(Route, $$"""{"card": {{card.ToJsonString()}}}""");

        var streams = await new YotoTrackStreams(new StubHttpClientFactory(handler)).GetStreamsAsync("abc12", CancellationToken.None);

        Assert.Equal("https://secure-media.example.com/one.mp3?sig=1", streams[new TrackAddress("01", "01")].AbsoluteUri);
        Assert.Equal("https://secure-media.example.com/two.aac?sig=2", streams[new TrackAddress("02", "01")].AbsoluteUri);
    }

    [Fact]
    public async Task GetStreamsAsync_UnsignedReference_IsLeftOut()
    {
        var handler = new StubHttpHandler().On(Route, $$"""{"card": {{Samples.Card().ToJsonString()}}}""");

        var streams = await new YotoTrackStreams(new StubHttpClientFactory(handler)).GetStreamsAsync("abc12", CancellationToken.None);

        Assert.Empty(streams);
    }
}
