using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;
using OpenMyo.Core.Publishing;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Publishing;

public sealed class ArtworkLoaderTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FakeAudioMetadataReader _reader = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task LoadAsync_ImageFile_ReturnsBytesWithContentType()
    {
        var path = _temp.CreateFile("cover.JPG", [1, 2, 3]);

        var picture = await new ArtworkLoader(_reader).LoadAsync(ArtworkSource.File(path), CancellationToken.None);

        Assert.Equal("image/jpeg", picture!.ContentType);
        Assert.Equal<byte>([1, 2, 3], picture.Data.ToArray());
    }

    [Fact]
    public async Task LoadAsync_EmbeddedSource_ReadsPictureFromAudioFile()
    {
        var expected = new EmbeddedPicture(new byte[] { 7 }, "image/png");
        _reader.Pictures["/m/a.mp3"] = expected;

        var picture = await new ArtworkLoader(_reader).LoadAsync(ArtworkSource.Embedded("/m/a.mp3"), CancellationToken.None);

        Assert.Equal(expected, picture);
    }

    [Fact]
    public async Task LoadAsync_EmbeddedSourceWithoutPicture_ReturnsNull()
    {
        var picture = await new ArtworkLoader(_reader).LoadAsync(ArtworkSource.Embedded("/m/a.mp3"), CancellationToken.None);

        Assert.Null(picture);
    }
}
