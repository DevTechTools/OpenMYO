using OpenMyo.Core.Playback;

namespace OpenMyo.Core.Tests.Playback;

public sealed class PlaybackListTests
{
    private static readonly IReadOnlyList<PlaybackItem> Three =
    [
        new("a", "A", new Uri("https://example.com/a"), TimeSpan.FromMinutes(1)),
        new("b", "B", new Uri("https://example.com/b"), TimeSpan.FromMinutes(1)),
        new("c", "C", new Uri("https://example.com/c"), TimeSpan.FromMinutes(1)),
    ];

    [Fact]
    public void MoveNext_AtLastItem_ReturnsFalseAndStays()
    {
        var queue = new PlaybackList(Three, 2);

        Assert.False(queue.MoveNext());
        Assert.Equal("c", queue.Current.RowKey);
    }

    [Fact]
    public void MoveNext_InMiddle_AdvancesOne()
    {
        var queue = new PlaybackList(Three, 0);

        Assert.True(queue.MoveNext());
        Assert.Equal("b", queue.Current.RowKey);
    }

    [Fact]
    public void MovePrevious_NearStartOfTrack_GoesBack()
    {
        var queue = new PlaybackList(Three, 1);

        Assert.True(queue.MovePrevious(TimeSpan.FromSeconds(1)));
        Assert.Equal("a", queue.Current.RowKey);
    }

    [Fact]
    public void MovePrevious_LaterInTrack_StaysForRestart()
    {
        var queue = new PlaybackList(Three, 1);

        Assert.False(queue.MovePrevious(TimeSpan.FromSeconds(10)));
        Assert.Equal("b", queue.Current.RowKey);
    }

    [Fact]
    public void Constructor_StartOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackList(Three, 3));
    }
}
