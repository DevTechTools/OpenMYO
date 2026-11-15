using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Import;

public sealed class ArtworkLocatorTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    private static ScannedFile Track(string path, string? pictureHash = null) =>
        new(path, AudioMetadata.Empty with { PictureHash = pictureHash });

    [Theory]
    [InlineData("cover.jpg")]
    [InlineData("Folder.PNG")]
    [InlineData("front.jpeg")]
    public void FindCover_ConventionallyNamedImage_IsUsed(string imageName)
    {
        var track = Track(_temp.CreateFile("01 Song.mp3"));
        var image = _temp.CreateFile(imageName);
        _temp.CreateFile("zzz.png");

        var cover = ArtworkLocator.FindCover(_temp.Path, [track]);

        Assert.Equal(ArtworkSource.File(image), cover);
    }

    [Fact]
    public void FindCover_SingleNonSidecarImage_IsUsed()
    {
        var track = Track(_temp.CreateFile("01 Song.mp3"));
        _temp.CreateFile("01 Song.png");
        var image = _temp.CreateFile("something.jpg");

        Assert.Equal(ArtworkSource.File(image), ArtworkLocator.FindCover(_temp.Path, [track]));
    }

    [Fact]
    public void FindCover_SeveralUnnamedImagesAndEmbeddedArt_UsesEmbedded()
    {
        var track = Track(_temp.CreateFile("01 Song.mp3"), pictureHash: "h1");
        _temp.CreateFile("a.jpg");
        _temp.CreateFile("b.jpg");

        Assert.Equal(ArtworkSource.Embedded(track.Path), ArtworkLocator.FindCover(_temp.Path, [track]));
    }

    [Fact]
    public void FindCover_NoImagesAndNoEmbeddedArt_ReturnsNull()
    {
        var track = Track(_temp.CreateFile("01 Song.mp3"));

        Assert.Null(ArtworkLocator.FindCover(_temp.Path, [track]));
    }

    [Fact]
    public void FindCover_MissingFolder_FallsBackToEmbeddedArt()
    {
        var track = Track("/nowhere/a.mp3", pictureHash: "h1");

        Assert.Equal(ArtworkSource.Embedded(track.Path), ArtworkLocator.FindCover(_temp.Combine("missing"), [track]));
    }

    [Fact]
    public void FindTrackIcons_SidecarImageWithSameBaseName_IsMatchedPerTrack()
    {
        var first = Track(_temp.CreateFile("01 Song.mp3"));
        var second = Track(_temp.CreateFile("02 Other.mp3"));
        var sidecar = _temp.CreateFile("01 Song.png");

        var icons = ArtworkLocator.FindTrackIcons(_temp.Path, [first, second]);

        Assert.Equal([ArtworkSource.File(sidecar), null], icons);
    }

    [Fact]
    public void FindTrackIcons_DistinctEmbeddedPictures_UseEmbeddedArt()
    {
        var first = Track(_temp.CreateFile("a.mp3"), "h1");
        var second = Track(_temp.CreateFile("b.mp3"), "h2");

        var icons = ArtworkLocator.FindTrackIcons(_temp.Path, [first, second]);

        Assert.Equal([ArtworkSource.Embedded(first.Path), ArtworkSource.Embedded(second.Path)], icons);
    }

    [Fact]
    public void FindTrackIcons_SharedEmbeddedPicture_IsTreatedAsCoverNotIcon()
    {
        var first = Track(_temp.CreateFile("a.mp3"), "same");
        var second = Track(_temp.CreateFile("b.mp3"), "same");

        Assert.All(ArtworkLocator.FindTrackIcons(_temp.Path, [first, second]), Assert.Null);
    }
}
