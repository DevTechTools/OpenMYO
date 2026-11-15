using OpenMyo.Core.Players;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Playback;

public class PlayTargetsTests
{
    private static readonly string[] Keys = ["01", "02", "03"];

    private static Playlist MakePlaylist(string? cardId = "card") => new()
    {
        CardId = cardId,
        Title = "Bedtime",
        Chapters = Keys
            .Select(key => new Chapter
            {
                Key = key,
                Title = "Chapter " + key,
                Tracks = [new Track { Key = "t" + key, Title = "T", TrackUrl = "yoto:#x" }],
            })
            .ToList(),
    };

    [Fact]
    public void ForChapter_SavedPlaylist_StartsAtChaptersFirstTrack()
    {
        var target = PlayTargets.ForChapter(MakePlaylist(), "02", TimeSpan.FromSeconds(5));

        Assert.Equal(new PlayTarget("card", "02", "t02", TimeSpan.FromSeconds(5)), target);
    }

    [Fact]
    public void ForChapter_UnsavedPlaylist_ReturnsNull()
    {
        Assert.Null(PlayTargets.ForChapter(MakePlaylist(cardId: null), "02", TimeSpan.Zero));
    }

    [Theory]
    [InlineData("02", 1, "03")]
    [InlineData("02", -1, "01")]
    [InlineData("03", 1, null)]
    [InlineData("01", -1, null)]
    [InlineData("99", 1, null)]
    public void Neighbour_Offset_ReturnsChapterOrNullPastTheEnds(string key, int offset, string? expected)
    {
        Assert.Equal(expected, PlayTargets.Neighbour(MakePlaylist(), key, offset));
    }
}
