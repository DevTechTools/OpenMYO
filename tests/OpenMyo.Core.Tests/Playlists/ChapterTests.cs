using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Playlists;

public sealed class ChapterTests
{
    [Fact]
    public void WithTitle_SingleTrack_TrimsAndRenamesChapterAndTrack()
    {
        var chapter = Chapter("Old", Track("Old"));

        var renamed = chapter.WithTitle("  New  ");

        Assert.Equal("New", renamed.Title);
        Assert.Equal("New", Assert.Single(renamed.Tracks).Title);
    }

    [Fact]
    public void WithTitle_SeveralTracks_KeepsTrackTitles()
    {
        var chapter = Chapter("Old", Track("Part 1"), Track("Part 2"));

        var renamed = chapter.WithTitle("New");

        Assert.Equal("New", renamed.Title);
        Assert.Equal(["Part 1", "Part 2"], renamed.Tracks.Select(track => track.Title));
    }

    [Fact]
    public void WithTitle_KeepsEverythingElse()
    {
        var chapter = Chapter("Old", Track("Old")) with { IconMediaId = "icon" };

        var renamed = chapter.WithTitle("New");

        Assert.Equal(chapter with { Title = "New", Tracks = renamed.Tracks }, renamed);
        Assert.Equal(chapter.Tracks[0] with { Title = "New" }, renamed.Tracks[0]);
    }

    private static Chapter Chapter(string title, params Track[] tracks) => new() { Key = "1", Title = title, Tracks = tracks };

    private static Track Track(string title) => new() { Key = title, Title = title, TrackUrl = "yoto:#sha" };
}
