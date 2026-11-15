using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;

namespace OpenMyo.Core.Tests.Import;

public class TrackOrdererTests
{
    private static ScannedFile File(string path, uint? track = null, uint? disc = null) =>
        new(path, new AudioMetadata(null, null, track, disc, TimeSpan.Zero, null));

    private static IEnumerable<string> Names(IReadOnlyList<ScannedFile> files) =>
        files.Select(file => System.IO.Path.GetFileName(file.Path));

    [Fact]
    public void Order_CompleteUniqueTagNumbers_UsesTagsOverFileNames()
    {
        var files = new[] { File("/m/01 a.mp3", track: 2), File("/m/02 b.mp3", track: 1) };

        Assert.Equal(["02 b.mp3", "01 a.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_DuplicateTagNumbers_FallsBackToFileNameNumbers()
    {
        var files = new[] { File("/m/02 x.mp3", track: 1), File("/m/01 y.mp3", track: 1) };

        Assert.Equal(["01 y.mp3", "02 x.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_SomeTagNumbersMissing_FallsBackToFileNameNumbers()
    {
        var files = new[] { File("/m/02 x.mp3", track: 1), File("/m/01 y.mp3") };

        Assert.Equal(["01 y.mp3", "02 x.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_FileNameNumbersCompareNumerically_NotAlphabetically()
    {
        var files = new[] { File("/m/10 b.mp3"), File("/m/2 a.mp3") };

        Assert.Equal(["2 a.mp3", "10 b.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_NoNumbersAnywhere_UsesNaturalFileNameOrder()
    {
        var files = new[] { File("/m/song 10.mp3"), File("/m/song 2.mp3"), File("/m/apple.mp3") };

        Assert.Equal(["apple.mp3", "song 2.mp3", "song 10.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_OnlySomeFileNamesNumbered_UsesNaturalFileNameOrder()
    {
        var files = new[] { File("/m/02 b.mp3"), File("/m/zebra.mp3"), File("/m/10 a.mp3") };

        Assert.Equal(["02 b.mp3", "10 a.mp3", "zebra.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_DiscFolders_OrdersByDiscThenTrack()
    {
        var files = new[] { File("/m/CD 2/01 a.mp3"), File("/m/CD 1/02 b.mp3"), File("/m/CD 1/01 c.mp3") };

        Assert.Equal(["01 c.mp3", "02 b.mp3", "01 a.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_DiscAndTrackTags_OrdersByDiscThenTrack()
    {
        var files = new[] { File("/m/a.mp3", track: 1, disc: 2), File("/m/b.mp3", track: 2, disc: 1), File("/m/c.mp3", track: 1, disc: 1) };

        Assert.Equal(["c.mp3", "b.mp3", "a.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_SameTrackNumberOnDifferentDiscs_CountsAsUniqueTags()
    {
        var files = new[] { File("/m/z.mp3", track: 1, disc: 2), File("/m/a.mp3", track: 1, disc: 1) };

        Assert.Equal(["a.mp3", "z.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_DiscTrackFileNames_UsesTrackPart()
    {
        var files = new[] { File("/m/1-02 b.mp3"), File("/m/1-01 a.mp3") };

        Assert.Equal(["1-01 a.mp3", "1-02 b.mp3"], Names(TrackOrderer.Order(files)));
    }

    [Fact]
    public void Order_Empty_ReturnsEmpty()
    {
        Assert.Empty(TrackOrderer.Order([]));
    }
}
