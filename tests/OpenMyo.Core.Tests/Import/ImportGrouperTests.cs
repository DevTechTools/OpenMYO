using OpenMyo.Core.Import;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Import;

public sealed class ImportGrouperTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("CD1", true)]
    [InlineData("Disc 2", true)]
    [InlineData("disk_03", true)]
    [InlineData("cd-4", true)]
    [InlineData("CD", false)]
    [InlineData("CD 123", false)]
    [InlineData("Discography", false)]
    [InlineData("Album", false)]
    public void IsDiscFolder_FolderName_MatchesDiscPattern(string name, bool expected)
    {
        Assert.Equal(expected, ImportGrouper.IsDiscFolder(name));
    }

    [Fact]
    public void Group_FolderWithAudio_BecomesOneGroupOfAudioOnly()
    {
        _temp.CreateFile("Album/01 a.mp3");
        _temp.CreateFile("Album/02 b.M4A");
        _temp.CreateFile("Album/cover.jpg");
        _temp.CreateFile("Album/notes.txt");
        _temp.CreateFile("Album/.hidden.mp3");

        var groups = ImportGrouper.Group([_temp.Combine("Album")]);

        var group = Assert.Single(groups);
        Assert.Equal(_temp.Combine("Album"), group.Folder);
        Assert.Equal(2, group.AudioFiles.Count);
    }

    [Fact]
    public void Group_DiscSubfolders_MergeIntoParent()
    {
        _temp.CreateFile("Box/CD1/a.mp3");
        _temp.CreateFile("Box/CD2/b.mp3");

        var group = Assert.Single(ImportGrouper.Group([_temp.Combine("Box")]));

        Assert.Equal(_temp.Combine("Box"), group.Folder);
        Assert.Equal(2, group.AudioFiles.Count);
    }

    [Fact]
    public void Group_NestedAlbums_BecomeSeparateGroupsInNaturalOrder()
    {
        _temp.CreateFile("Artist/Album 10/a.mp3");
        _temp.CreateFile("Artist/Album 2/b.mp3");

        var groups = ImportGrouper.Group([_temp.Combine("Artist")]);

        Assert.Equal([_temp.Combine("Artist/Album 2"), _temp.Combine("Artist/Album 10")], groups.Select(group => group.Folder));
    }

    [Fact]
    public void Group_LooseFiles_GroupByFolderAndSkipNonAudio()
    {
        var first = _temp.CreateFile("A/one.mp3");
        var second = _temp.CreateFile("A/two.mp3");
        var other = _temp.CreateFile("B/three.flac");
        var text = _temp.CreateFile("B/readme.txt");

        var groups = ImportGrouper.Group([first, second, other, text]);

        Assert.Equal([2, 1], groups.Select(group => group.AudioFiles.Count));
    }

    [Fact]
    public void Group_SameFileSelectedTwice_ListedOnce()
    {
        var file = _temp.CreateFile("A/one.mp3");

        var group = Assert.Single(ImportGrouper.Group([file, file, _temp.Combine("A")]));

        Assert.Single(group.AudioFiles);
    }

    [Fact]
    public void Group_MissingPath_ReturnsNoGroups()
    {
        Assert.Empty(ImportGrouper.Group([_temp.Combine("nope")]));
    }
}
