using OpenMyo.Core.Library;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Library;

public class PlaylistHealthTests
{
    private static readonly Uri Cover = new("https://example.test/c.png");

    private static Chapter MakeChapter(string? iconId, int trackCount = 1) => new()
    {
        Key = "01",
        Title = "Chapter",
        IconMediaId = iconId,
        Tracks = Enumerable.Range(0, trackCount)
            .Select(i => new Track { Key = i.ToString(System.Globalization.CultureInfo.InvariantCulture), Title = "T", TrackUrl = "yoto:#x" })
            .ToList(),
    };

    [Fact]
    public void Evaluate_CompletePlaylist_HasNoIssues()
    {
        var playlist = new Playlist { Title = "P", CoverImageUrl = Cover, Chapters = [MakeChapter("icon")] };

        Assert.Equal(PlaylistIssues.None, PlaylistHealth.Evaluate(playlist));
    }

    [Fact]
    public void Evaluate_EmptyPlaylistWithoutCover_ReportsNoTracksAndMissingCover()
    {
        var playlist = new Playlist { Title = "P" };

        Assert.Equal(PlaylistIssues.NoTracks | PlaylistIssues.MissingCover, PlaylistHealth.Evaluate(playlist));
    }

    [Fact]
    public void Evaluate_ChaptersWithoutTracks_ReportsNoTracks()
    {
        var playlist = new Playlist { Title = "P", CoverImageUrl = Cover, Chapters = [MakeChapter("icon", trackCount: 0)] };

        Assert.True(PlaylistHealth.Evaluate(playlist).HasFlag(PlaylistIssues.NoTracks));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Evaluate_ChapterWithoutIcon_ReportsMissingIcons(string? iconId)
    {
        var playlist = new Playlist { Title = "P", CoverImageUrl = Cover, Chapters = [MakeChapter("icon"), MakeChapter(iconId)] };

        Assert.Equal(PlaylistIssues.MissingIcons, PlaylistHealth.Evaluate(playlist));
    }

    [Fact]
    public void Evaluate_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PlaylistHealth.Evaluate(null!));
    }
}
