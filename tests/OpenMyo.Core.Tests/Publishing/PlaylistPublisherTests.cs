using OpenMyo.Core.Icons;
using OpenMyo.Core.Import;
using OpenMyo.Core.Playlists;
using OpenMyo.Core.Publishing;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Publishing;

public sealed class PlaylistPublisherTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FakeMediaUploader _uploader = new();
    private readonly FakePlaylistRepository _repository = new();
    private readonly FakeAudioMetadataReader _reader = new();
    private readonly FakeIconCatalog _catalog = new([]);

    public void Dispose() => _temp.Dispose();

    private PlaylistPublisher CreatePublisher(IconIndexProvider? provider = null) =>
        new(_uploader, _repository, new FakeSettingsStore(), new ArtworkLoader(_reader), provider ?? new IconIndexProvider(_catalog));

    private static TrackDraft Draft(string path, string title, ArtworkSource? art = null, string? iconId = null) =>
        new(path, title, TimeSpan.FromSeconds(5), art, iconId);

    [Fact]
    public async Task PublishAsync_Tracks_CreatesChaptersInOrderWithKeysEvenWhenUploadsFinishOutOfOrder()
    {
        _uploader.SlowPaths.Add("/m/a.mp3");
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "First"), Draft("/m/b.mp3", "Second")], null);

        var playlist = await CreatePublisher().PublishAsync(draft, null, CancellationToken.None);

        Assert.Equal(["01", "02"], playlist.Chapters.Select(chapter => chapter.Key));
        Assert.Equal(["First", "Second"], playlist.Chapters.Select(chapter => chapter.Title));
        var track = Assert.Single(playlist.Chapters[0].Tracks);
        Assert.Equal("01", track.Key);
        Assert.Equal("yoto:#a.mp3", track.TrackUrl);
        Assert.Equal("mp3", track.Format);
        Assert.Equal(100, track.FileSize);
        Assert.Equal("stereo", track.Channels);
        Assert.Equal("Album", playlist.Title);
        Assert.Equal("new-card", playlist.CardId);
    }

    [Fact]
    public async Task PublishAsync_IconMediaIdAndArtwork_IconMediaIdWinsAndNothingIsUploaded()
    {
        var image = _temp.CreateFile("a.png", [1, 2, 3]);
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A", ArtworkSource.File(image), "chosen")], null);

        var playlist = await CreatePublisher().PublishAsync(draft, null, CancellationToken.None);

        Assert.Equal("chosen", playlist.Chapters[0].IconMediaId);
        Assert.Equal("chosen", playlist.Chapters[0].Tracks[0].IconMediaId);
        Assert.Empty(_uploader.Icons);
    }

    [Fact]
    public async Task PublishAsync_ArtworkWithoutIconId_UploadsArtworkAsIcon()
    {
        var image = _temp.CreateFile("a.png", [1, 2, 3]);
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A", ArtworkSource.File(image))], null);

        var playlist = await CreatePublisher().PublishAsync(draft, null, CancellationToken.None);

        Assert.Equal(new UploadedIcon("a", "image/png", 3), Assert.Single(_uploader.Icons));
        Assert.Equal("uploaded-a", playlist.Chapters[0].IconMediaId);
    }

    [Fact]
    public async Task PublishAsync_EmbeddedArtworkWithoutPicture_LeavesChapterWithoutIcon()
    {
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A", ArtworkSource.Embedded("/m/a.mp3"))], null);

        var playlist = await CreatePublisher().PublishAsync(draft, null, CancellationToken.None);

        Assert.False(playlist.Chapters[0].HasIcon);
        Assert.Empty(_uploader.Icons);
    }

    [Fact]
    public async Task PublishAsync_UploadedIconArtwork_InvalidatesIconIndex()
    {
        var provider = new IconIndexProvider(_catalog);
        await provider.GetAsync(CancellationToken.None);
        var image = _temp.CreateFile("a.png", [1]);
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A", ArtworkSource.File(image))], null);

        await CreatePublisher(provider).PublishAsync(draft, null, CancellationToken.None);
        await provider.GetAsync(CancellationToken.None);

        Assert.Equal(2, _catalog.CallCount);
    }

    [Fact]
    public async Task PublishAsync_Cover_IsUploadedAndSetOnPlaylist()
    {
        var image = _temp.CreateFile("cover.jpg", [9, 9]);
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A")], ArtworkSource.File(image));

        var playlist = await CreatePublisher().PublishAsync(draft, null, CancellationToken.None);

        Assert.Equal(("image/jpeg", 2), Assert.Single(_uploader.Covers));
        Assert.Equal(FakeMediaUploader.CoverUrl, playlist.CoverImageUrl);
    }

    [Fact]
    public async Task PublishAsync_NoCover_UploadsNoCover()
    {
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A")], null);

        var playlist = await CreatePublisher().PublishAsync(draft, null, CancellationToken.None);

        Assert.Empty(_uploader.Covers);
        Assert.Null(playlist.CoverImageUrl);
    }

    [Fact]
    public async Task PublishAsync_Progress_EndsWithSavingStatus()
    {
        var progress = new RecordingProgress<PublishProgress>();
        var draft = new PlaylistDraft("Album", "/m", [Draft("/m/a.mp3", "A")], null);

        await CreatePublisher().PublishAsync(draft, progress, CancellationToken.None);

        Assert.Equal(new PublishProgress(0.99, "Saving playlist"), progress.Reports[^1]);
        Assert.Contains(progress.Reports, report => report.Status == "Uploading 1 of 1");
    }

    [Fact]
    public async Task AppendAsync_ExistingChapters_AppendsWithContinuingKeys()
    {
        var existing = new Playlist
        {
            CardId = "card-9",
            Title = "Old",
            Chapters = [new Chapter { Key = "01", Title = "One" }, new Chapter { Key = "02", Title = "Two" }],
        };

        var playlist = await CreatePublisher().AppendFilesAsync(existing, [Draft("/m/c.mp3", "Three")], null, CancellationToken.None);

        Assert.Equal("card-9", playlist.CardId);
        Assert.Equal(["01", "02", "03"], playlist.Chapters.Select(chapter => chapter.Key));
        Assert.Equal(["One", "Two", "Three"], playlist.Chapters.Select(chapter => chapter.Title));
        Assert.Same(playlist.Chapters, _repository.Saved[0].Chapters);
    }
}
