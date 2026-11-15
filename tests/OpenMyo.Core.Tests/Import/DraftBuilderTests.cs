using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Import;

public sealed class DraftBuilderTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FakeAudioMetadataReader _reader = new();

    public void Dispose() => _temp.Dispose();

    private static AudioMetadata Tags(string? title = null, string? album = null, uint? track = null, int seconds = 0, string? picture = null) =>
        new(title, album, track, null, TimeSpan.FromSeconds(seconds), picture);

    private Task<PlaylistDraft> BuildFolderAsync(string folderName) =>
        new DraftBuilder(_reader).BuildAsync(
            Assert.Single(ImportGrouper.Group([_temp.Combine(folderName)])), CancellationToken.None);

    [Fact]
    public async Task BuildAsync_TagTitles_AreCleanedAndOrderedByTagNumber()
    {
        var second = _temp.CreateFile("Album/a.mp3");
        var first = _temp.CreateFile("Album/b.mp3");
        _reader.Tags[second] = Tags("Second Song", track: 2, seconds: 20);
        _reader.Tags[first] = Tags("First Song", track: 1, seconds: 10);

        var draft = await BuildFolderAsync("Album");

        Assert.Equal(["First Song", "Second Song"], draft.Tracks.Select(track => track.Title));
        Assert.Equal([TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20)], draft.Tracks.Select(track => track.Duration));
        Assert.Equal([first, second], draft.Tracks.Select(track => track.SourcePath));
    }

    [Fact]
    public async Task BuildAsync_PlaceholderTagTitle_UsesFileName()
    {
        var file = _temp.CreateFile("Album/01_-_moon_song.mp3");
        _reader.Tags[file] = Tags("Track 01");

        var draft = await BuildFolderAsync("Album");

        Assert.Equal("Moon Song", Assert.Single(draft.Tracks).Title);
    }

    [Fact]
    public async Task BuildAsync_NoTags_UsesCleanedFileNames()
    {
        _temp.CreateFile("Album/01 - Alpha.mp3");
        _temp.CreateFile("Album/02 - Beta.mp3");

        var draft = await BuildFolderAsync("Album");

        Assert.Equal(["Alpha", "Beta"], draft.Tracks.Select(track => track.Title));
    }

    [Fact]
    public async Task BuildAsync_AllTracksShareAlbumTag_AlbumBecomesPlaylistTitle()
    {
        _reader.Tags[_temp.CreateFile("Folder/a.mp3")] = Tags(album: "The Album");
        _reader.Tags[_temp.CreateFile("Folder/b.mp3")] = Tags(album: "the album");

        var draft = await BuildFolderAsync("Folder");

        Assert.Equal("The Album", draft.Title);
        Assert.Equal(_temp.Combine("Folder"), draft.SourceFolder);
    }

    [Fact]
    public async Task BuildAsync_AlbumTagsDisagree_FolderNameBecomesPlaylistTitle()
    {
        _reader.Tags[_temp.CreateFile("my_folder/a.mp3")] = Tags(album: "One");
        _reader.Tags[_temp.CreateFile("my_folder/b.mp3")] = Tags(album: "Two");

        var draft = await BuildFolderAsync("my_folder");

        Assert.Equal("My Folder", draft.Title);
    }

    [Fact]
    public async Task BuildAsync_FolderWithoutName_UsesDefaultTitle()
    {
        var group = new ImportGroup(string.Empty, ["/nowhere/a.mp3"]);

        var draft = await new DraftBuilder(_reader).BuildAsync(group, CancellationToken.None);

        Assert.Equal("New Playlist", draft.Title);
    }

    [Fact]
    public async Task BuildAsync_CoverAndSidecarImages_AreAttached()
    {
        _temp.CreateFile("Album/01 Song.mp3");
        var cover = _temp.CreateFile("Album/cover.jpg");
        var sidecar = _temp.CreateFile("Album/01 Song.png");

        var draft = await BuildFolderAsync("Album");

        Assert.Equal(ArtworkSource.File(cover), draft.Cover);
        Assert.Equal(ArtworkSource.File(sidecar), Assert.Single(draft.Tracks).IconArtwork);
    }

    [Fact]
    public async Task BuildAsync_SelectedPaths_ReturnsOneDraftPerFolder()
    {
        _temp.CreateFile("A/one.mp3");
        _temp.CreateFile("B/two.mp3");

        var drafts = await new DraftBuilder(_reader).BuildAsync([_temp.Path], CancellationToken.None);

        Assert.Equal(["A", "B"], drafts.Select(draft => draft.Title));
    }

    [Fact]
    public async Task BuildAsync_NullGroup_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new DraftBuilder(_reader).BuildAsync((ImportGroup)null!, CancellationToken.None));
    }
}
