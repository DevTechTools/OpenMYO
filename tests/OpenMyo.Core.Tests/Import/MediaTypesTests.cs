using OpenMyo.Core.Import;

namespace OpenMyo.Core.Tests.Import;

public class MediaTypesTests
{
    [Theory]
    [InlineData("a.MP3", "audio/mpeg")]
    [InlineData("a.m4b", "audio/mp4")]
    [InlineData("a.jpeg", "image/jpeg")]
    [InlineData("a.PNG", "image/png")]
    [InlineData("a.txt", "application/octet-stream")]
    [InlineData("noextension", "application/octet-stream")]
    public void ContentTypeOf_Path_ReturnsMimeType(string path, string expected)
    {
        Assert.Equal(expected, MediaTypes.ContentTypeOf(path));
    }

    [Fact]
    public void IsAudioAndIsImage_Paths_ClassifyByExtension()
    {
        Assert.True(MediaTypes.IsAudio("x.flac"));
        Assert.False(MediaTypes.IsAudio("x.png"));
        Assert.True(MediaTypes.IsImage("x.gif"));
        Assert.False(MediaTypes.IsImage("x.mp3"));
    }
}
