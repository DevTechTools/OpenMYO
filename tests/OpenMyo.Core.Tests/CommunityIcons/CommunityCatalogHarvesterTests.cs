using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.CommunityIcons;

public sealed class CommunityCatalogHarvesterTests
{
    [Fact]
    public async Task HarvestAsync_TotalKnown_ReadsEveryPageWithBoundedParallelism()
    {
        var source = new FakeCommunityIconSource(pageCount: 20) { Delay = TimeSpan.FromMilliseconds(20) };

        var catalog = await new CommunityCatalogHarvester(source).HarvestAsync(null, CancellationToken.None);

        Assert.Equal(60, catalog.Icons.Count);
        Assert.True(catalog.IsComplete);
        Assert.Equal(20, source.TotalCalls);
        Assert.InRange(source.PeakConcurrency, 2, CommunityCatalogHarvester.MaxParallelPages);
    }

    [Fact]
    public async Task HarvestAsync_PageFailsOnce_RetriesAndCompletes()
    {
        var source = new FakeCommunityIconSource(pageCount: 5);
        source.Failures[3] = 1;

        var catalog = await new CommunityCatalogHarvester(source).HarvestAsync(null, CancellationToken.None);

        Assert.True(catalog.IsComplete);
        Assert.Equal(15, catalog.Icons.Count);
        Assert.Equal(2, source.CallsFor(3));
    }

    [Fact]
    public async Task HarvestAsync_PageAlwaysFails_KeepsTheRestAndMarksIncomplete()
    {
        var source = new FakeCommunityIconSource(pageCount: 5);
        source.Failures[4] = int.MaxValue;

        var catalog = await new CommunityCatalogHarvester(source).HarvestAsync(null, CancellationToken.None);

        Assert.False(catalog.IsComplete);
        Assert.Equal(12, catalog.Icons.Count);
        Assert.Equal(3, source.CallsFor(4));
    }

    [Fact]
    public async Task HarvestAsync_NoTotalOnPage_ReadsUntilAnEmptyPage()
    {
        var source = new FakeCommunityIconSource(pageCount: 6) { ReportsTotal = false };

        var catalog = await new CommunityCatalogHarvester(source).HarvestAsync(null, CancellationToken.None);

        Assert.True(catalog.IsComplete);
        Assert.Equal(18, catalog.Icons.Count);
    }

    [Fact]
    public async Task HarvestAsync_ReportsProgress_EndingAtAllPages()
    {
        var source = new FakeCommunityIconSource(pageCount: 8);
        var progress = new RecordingProgress<CommunityHarvestProgress>();

        await new CommunityCatalogHarvester(source).HarvestAsync(progress, CancellationToken.None);

        Assert.Equal(8, progress.Reports.Count);
        Assert.Contains(progress.Reports, report => report is { PagesDone: 8, TotalPages: 8, Fraction: 1.0 });
    }

    [Fact]
    public async Task HarvestAsync_FirstPageUnreachable_Throws()
    {
        var source = new FakeCommunityIconSource(pageCount: 3);
        source.Failures[1] = int.MaxValue;

        await Assert.ThrowsAsync<HttpRequestException>(
            () => new CommunityCatalogHarvester(source).HarvestAsync(null, CancellationToken.None));
    }
}
