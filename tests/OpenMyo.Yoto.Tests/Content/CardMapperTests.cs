using OpenMyo.Yoto.Content;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Tests.Content;

public sealed class CardMapperTests
{
    [Fact]
    public void ToPlaylist_SampleCard_MapsFields()
    {
        var playlist = CardMapper.ToPlaylist(Samples.Card());

        Assert.Equal("abc12", playlist.CardId);
        Assert.Equal("Bedtime", playlist.Title);
        Assert.Equal("https://example.com/c.png", playlist.CoverImageUrl?.AbsoluteUri);
        Assert.Equal(new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero), playlist.UpdatedAt);
        Assert.Equal(2, playlist.Chapters.Count);
    }

    [Fact]
    public void ToPlaylist_DurationsInSeconds_BecomeTimeSpans()
    {
        var playlist = CardMapper.ToPlaylist(Samples.Card());

        Assert.Equal(TimeSpan.FromSeconds(10.5), playlist.Chapters[0].Tracks[0].Duration);
        Assert.Equal(TimeSpan.FromSeconds(20), playlist.Chapters[1].Tracks[0].Duration);
        Assert.Equal(200, playlist.Chapters[1].Tracks[0].FileSize);
    }

    [Fact]
    public void ToPlaylist_ChapterWithoutIcon_FallsBackToFirstTrackIcon()
    {
        var card = Samples.Card();
        card["content"]!["chapters"]![1]!["display"] = null;

        var chapter = CardMapper.ToPlaylist(card).Chapters[1];

        Assert.Equal(Samples.IconB, chapter.IconMediaId);
    }

    [Fact]
    public void ToPlaylist_ChapterIcon_WinsOverTrackIcon()
    {
        var chapter = CardMapper.ToPlaylist(Samples.Card()).Chapters[1];

        Assert.Equal(Samples.IconA, chapter.IconMediaId);
        Assert.False(CardMapper.ToPlaylist(Samples.Card()).Chapters[0].HasIcon);
    }

    [Fact]
    public void ToPlaylist_ChannelsAsNumberOrText_MapToText()
    {
        var playlist = CardMapper.ToPlaylist(Samples.Card());

        Assert.Equal("stereo", playlist.Chapters[0].Tracks[0].Channels);
        Assert.Equal("mono", playlist.Chapters[1].Tracks[0].Channels);
    }

    [Fact]
    public void ToSummary_ValidItem_MapsCover()
    {
        var item = new CardListItem
        {
            CardId = "abc12",
            Title = "T",
            UpdatedAt = "2025-01-02T03:04:05Z",
            Metadata = new CardListMetadata { Cover = new CardListCover { ImageL = "https://example.com/x.png" } },
        };

        var summary = CardMapper.ToSummary(item);

        Assert.Equal("T", summary?.Title);
        Assert.Equal("https://example.com/x.png", summary?.CoverImageUrl?.AbsoluteUri);
    }

    [Fact]
    public void ToSummary_InvalidCardId_ReturnsNull() =>
        Assert.Null(CardMapper.ToSummary(new CardListItem { CardId = "../x" }));
}
