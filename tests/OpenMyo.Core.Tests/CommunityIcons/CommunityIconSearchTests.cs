using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.CommunityIcons;

public sealed class CommunityIconSearchTests
{
    [Fact]
    public async Task SearchAsync_TrigramMatch_RanksFirstThenPopularKeywordMatches()
    {
        var search = Search(
            FakeCommunityIconSource.Icon("1", "taco", 5000),
            FakeCommunityIconSource.Icon("2", "red dragon", 10),
            FakeCommunityIconSource.Icon("3", "dragonfly", 300));

        var results = await search.SearchAsync("The Red Dragon", 10, CancellationToken.None);

        Assert.Equal("2", results[0].Id);
        Assert.Contains(results, icon => icon.Id == "3");
        Assert.DoesNotContain(results, icon => icon.Id == "1");
    }

    [Fact]
    public void Search_SubstringOnlyMatches_FillByDownloads()
    {
        var index = CommunityCatalogIndex.Build(new CommunityCatalog(
            [
                FakeCommunityIconSource.Icon("1", "superdragonish", 5),
                FakeCommunityIconSource.Icon("2", "megadragonish", 900),
            ],
            DateTimeOffset.UtcNow,
            IsComplete: true));

        var results = index.Search("dragon", 10);

        Assert.Equal(["2", "1"], results.Select(icon => icon.Id));
    }

    [Fact]
    public async Task SearchAsync_NoKeywords_DoesNotStartHarvest()
    {
        var source = new FakeCommunityIconSource(pageCount: 2);
        var provider = new CommunityCatalogProvider(new CommunityCatalogHarvester(source), new FakeCommunityCatalogCache());

        var results = await new CommunityIconSearch(provider).SearchAsync("the 01", 10, CancellationToken.None);

        Assert.Empty(results);
        Assert.Equal(0, source.TotalCalls);
    }

    private static CommunityIconSearch Search(params CommunityIcon[] icons)
    {
        var cache = new FakeCommunityCatalogCache(new CommunityCatalog(icons, DateTimeOffset.UtcNow, IsComplete: true));
        var provider = new CommunityCatalogProvider(new CommunityCatalogHarvester(new FakeCommunityIconSource(0)), cache);
        return new CommunityIconSearch(provider);
    }
}
