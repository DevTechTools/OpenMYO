using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Feeds;
using OpenMyo.Core.Import;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Feeds;

public sealed class FeedImporterTests : IDisposable
{
    private static readonly Uri FeedAddress = new("https://example.com/feed.xml");
    private static readonly Uri Art = new("https://example.com/art");
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A];

    private readonly TempDirectory _temp = new();
    private readonly FakeFeedClient _client = new();
    private readonly FakeAudioMetadataReader _reader = new();
    private readonly FeedImporter _importer;

    public FeedImporterTests() => _importer = ImporterWith(new AppSettings());

    private FeedImporter ImporterWith(AppSettings settings) =>
        new(_client, _reader, new FakeAppPaths(_temp.Path), new FakeSettingsStore(settings));

    public void Dispose() => _temp.Dispose();

    private static FeedEpisode Episode(string title, int day) =>
        new(title, new Uri($"https://cdn.example.com/{day}.mp3"), new DateTimeOffset(2025, 6, day, 0, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(day), "audio/mpeg");

    private PodcastFeed Feed(Uri? image, params FeedEpisode[] episodes)
    {
        foreach (var episode in episodes)
        {
            _client.Files[episode.AudioUrl] = [1, 2, 3];
        }

        return new PodcastFeed(FeedAddress, "Bedtime Tales", image, episodes);
    }

    [Fact]
    public async Task DownloadAsync_SelectedEpisodes_BecomeTracksInFeedOrder()
    {
        var (one, two, three) = (Episode("01 - The Owl", 1), Episode("02 - The Fox", 2), Episode("03 - The Bear", 3));
        var feed = Feed(null, one, two, three);

        var draft = await _importer.DownloadAsync(feed, [three, one], EpisodeOrder.OldestFirst, progress: null, CancellationToken.None);

        Assert.Equal("Bedtime Tales", draft.Title);
        Assert.Equal(["The Owl", "The Bear"], draft.Tracks.Select(track => track.Title));
        Assert.Equal(["001.mp3", "002.mp3"], draft.Tracks.Select(track => Path.GetFileName(track.SourcePath)));
        Assert.All(draft.Tracks, track => Assert.True(File.Exists(track.SourcePath)));
        Assert.Equal([TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3)], draft.Tracks.Select(track => track.Duration));
    }

    [Fact]
    public async Task DownloadAsync_NewestFirst_ReversesTrackOrder()
    {
        var (one, two) = (Episode("One", 1), Episode("Two", 2));

        var draft = await _importer.DownloadAsync(Feed(null, one, two), [one, two], EpisodeOrder.NewestFirst, progress: null, CancellationToken.None);

        Assert.Equal(["Two", "One"], draft.Tracks.Select(track => track.Title));
        Assert.Equal("001.mp3", Path.GetFileName(draft.Tracks[0].SourcePath));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(50, 8)]
    public async Task DownloadAsync_ParallelSetting_LimitsDownloadsAtOnce(int setting, int expected)
    {
        var episodes = Enumerable.Range(1, 10).Select(i => Episode($"E{i}", i)).ToArray();
        var feed = Feed(null, episodes);
        _client.DownloadDelay = TimeSpan.FromMilliseconds(50);

        await ImporterWith(new AppSettings { MaxParallelDownloads = setting })
            .DownloadAsync(feed, episodes, EpisodeOrder.OldestFirst, progress: null, CancellationToken.None);

        Assert.Equal(expected, _client.MostAtOnce);
    }

    [Fact]
    public async Task DownloadAsync_Progress_NamesEachEpisode()
    {
        var (one, two) = (Episode("One", 1), Episode("Two", 2));
        var progress = new RecordingProgress<FeedDownloadProgress>();

        await _importer.DownloadAsync(Feed(null, one, two), [one, two], EpisodeOrder.OldestFirst, progress, CancellationToken.None);
        var updates = progress.Reports;

        Assert.Contains(updates, update => update.Episode == one && update.EpisodeFraction >= 1);
        Assert.Contains(updates, update => update.Episode == two && update.EpisodeFraction >= 1);
        Assert.Equal(1, updates[^1].Fraction);
    }

    [Fact]
    public async Task DownloadAsync_FeedImage_BecomesCoverWithDetectedType()
    {
        var one = Episode("One", 1);
        var feed = Feed(Art, one);
        _client.Files[Art] = Png;

        var draft = await _importer.DownloadAsync(feed, [one], EpisodeOrder.OldestFirst, progress: null, CancellationToken.None);

        Assert.NotNull(draft.Cover);
        Assert.Equal("cover.png", Path.GetFileName(draft.Cover.Path));
        Assert.False(draft.Cover.IsEmbedded);
    }

    [Fact]
    public async Task DownloadAsync_BrokenImageLink_KeepsEpisodesWithoutCover()
    {
        var one = Episode("One", 1);

        var draft = await _importer.DownloadAsync(Feed(Art, one), [one], EpisodeOrder.OldestFirst, progress: null, CancellationToken.None);

        Assert.Null(draft.Cover);
        Assert.Single(draft.Tracks);
    }

    [Fact]
    public async Task DownloadAsync_EpisodeFails_RemovesDownloadFolder()
    {
        var (one, two) = (Episode("One", 1), Episode("Two", 2));
        var feed = Feed(null, one, two);
        _client.Files.Remove(two.AudioUrl);

        await Assert.ThrowsAsync<HttpRequestException>(() => _importer.DownloadAsync(feed, [one, two], EpisodeOrder.OldestFirst, progress: null, CancellationToken.None));

        Assert.Empty(Directory.EnumerateDirectories(Path.Combine(_temp.Path, "FeedDownloads")));
    }

    [Fact]
    public async Task DownloadAsync_TooManyEpisodes_Throws()
    {
        var episodes = Enumerable.Range(1, FeedImporter.MaxEpisodes + 1).Select(i => Episode($"E{i}", 1) with { AudioUrl = new Uri($"https://cdn.example.com/e{i}.mp3") }).ToArray();

        await Assert.ThrowsAsync<ArgumentException>(() => _importer.DownloadAsync(Feed(null, episodes), episodes, EpisodeOrder.OldestFirst, progress: null, CancellationToken.None));
    }

    [Fact]
    public async Task Discard_FeedDraft_DeletesItsFolder()
    {
        var one = Episode("One", 1);
        var draft = await _importer.DownloadAsync(Feed(null, one), [one], EpisodeOrder.OldestFirst, progress: null, CancellationToken.None);

        _importer.Discard(draft);

        Assert.False(Directory.Exists(draft.SourceFolder));
    }

    [Fact]
    public void Discard_DraftFromUserFolder_LeavesFilesAlone()
    {
        var file = _temp.CreateFile("My Album/01.mp3");
        var draft = new PlaylistDraft("My Album", Path.GetDirectoryName(file)!, [], null);

        _importer.Discard(draft);

        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task LoadAsync_ParsesFetchedFeed()
    {
        _client.FeedXml = """
            <rss version="2.0"><channel><title>Show</title>
            <item><title>One</title><enclosure url="https://cdn.example.com/1.mp3" type="audio/mpeg" /></item>
            </channel></rss>
            """;

        var feed = await _importer.LoadAsync(FeedAddress, CancellationToken.None);

        Assert.Equal(FeedAddress, feed.Url);
        Assert.Equal("One", Assert.Single(feed.Episodes).Title);
    }
}
