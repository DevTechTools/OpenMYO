using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.CommunityIcons;

public sealed class CommunityCatalogProviderTests
{
    [Fact]
    public async Task GetAsync_FreshCache_DoesNotFetch()
    {
        var source = new FakeCommunityIconSource(pageCount: 3);
        var cache = new FakeCommunityCatalogCache(Catalog(TimeSpan.FromDays(1), isComplete: true, "cached dragon"));

        var index = await Provider(source, cache).GetAsync(null, CancellationToken.None);

        Assert.Equal(1, index.Count);
        Assert.Equal(0, source.TotalCalls);
        Assert.Equal(0, cache.SaveCount);
    }

    [Fact]
    public async Task GetAsync_StaleCache_HarvestsAndSaves()
    {
        var source = new FakeCommunityIconSource(pageCount: 3);
        var cache = new FakeCommunityCatalogCache(Catalog(TimeSpan.FromDays(8), isComplete: true, "old"));

        var index = await Provider(source, cache).GetAsync(null, CancellationToken.None);

        Assert.Equal(9, index.Count);
        Assert.Equal(1, cache.SaveCount);
        Assert.True(cache.Stored?.IsComplete);
    }

    [Fact]
    public async Task GetAsync_RecentButIncompleteCache_IsNotTreatedAsFresh()
    {
        var source = new FakeCommunityIconSource(pageCount: 2);
        var cache = new FakeCommunityCatalogCache(Catalog(TimeSpan.FromHours(1), isComplete: false, "partial"));

        await Provider(source, cache).GetAsync(null, CancellationToken.None);

        Assert.Equal(2, source.TotalCalls);
    }

    [Fact]
    public async Task GetAsync_PartialHarvest_SavesAsIncompleteMergedWithStale()
    {
        var source = new FakeCommunityIconSource(pageCount: 3);
        source.Failures[2] = int.MaxValue;
        var cache = new FakeCommunityCatalogCache(Catalog(TimeSpan.FromDays(30), isComplete: true, "old"));

        var index = await Provider(source, cache).GetAsync(null, CancellationToken.None);

        Assert.False(cache.Stored?.IsComplete);
        Assert.Equal(7, index.Count);
    }

    [Fact]
    public async Task GetAsync_OfflineWithStaleCache_UsesStaleCatalog()
    {
        var source = new FakeCommunityIconSource(pageCount: 3);
        source.Failures[1] = int.MaxValue;
        var cache = new FakeCommunityCatalogCache(Catalog(TimeSpan.FromDays(30), isComplete: true, "old"));

        var index = await Provider(source, cache).GetAsync(null, CancellationToken.None);

        Assert.Equal(1, index.Count);
        Assert.Equal(0, cache.SaveCount);
    }

    [Fact]
    public async Task GetAsync_ConcurrentCallers_ShareOneHarvest()
    {
        var source = new FakeCommunityIconSource(pageCount: 5) { Delay = TimeSpan.FromMilliseconds(20) };
        var provider = Provider(source, new FakeCommunityCatalogCache());

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => provider.GetAsync(null, CancellationToken.None)));

        Assert.All(results, index => Assert.Same(results[0], index));
        Assert.Equal(5, source.TotalCalls);
        Assert.True(provider.IsReady);
        Assert.Equal(15, provider.Count);
    }

    [Fact]
    public async Task WarmUpAsync_WithProgress_ReportsHarvestedPages()
    {
        var provider = Provider(new FakeCommunityIconSource(pageCount: 4), new FakeCommunityCatalogCache());
        var progress = new RecordingProgress<CommunityHarvestProgress>();

        await provider.WarmUpAsync(progress, CancellationToken.None);

        Assert.Equal(4, progress.Reports.Max(report => report.PagesDone));
    }

    private static CommunityCatalogProvider Provider(FakeCommunityIconSource source, FakeCommunityCatalogCache cache) =>
        new(new CommunityCatalogHarvester(source), cache);

    private static CommunityCatalog Catalog(TimeSpan age, bool isComplete, string title) =>
        new([FakeCommunityIconSource.Icon("cached", title)], DateTimeOffset.UtcNow - age, isComplete);
}
