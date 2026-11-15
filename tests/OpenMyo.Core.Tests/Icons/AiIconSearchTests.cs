using OpenMyo.Core.Icons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Icons;

public sealed class AiIconSearchTests
{
    private static AiIconSearch Create(FakeKeywordGenerator? generator) =>
        new(new IconIndexProvider(new FakeIconCatalog([TestIcons.Make("owl", "Owl"), TestIcons.Make("moon", "Moon")])), generator);

    [Fact]
    public async Task SearchAsync_GeneratorKeywords_ReturnsMatchingIconsInKeywordOrder()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["moon", "owl"]] };

        var result = await Create(generator).SearchAsync("Goodnight Song", CancellationToken.None);

        Assert.Equal(["moon", "owl"], result.Keywords);
        Assert.Equal(["moon", "owl"], result.Matches.Select(match => match.Icon.MediaId));
    }

    [Fact]
    public async Task SearchAsync_SameTitleTwice_CallsGeneratorOnce()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["owl"]] };
        var search = Create(generator);

        await search.SearchAsync("Goodnight Song", CancellationToken.None);
        await search.SearchAsync("goodnight song", CancellationToken.None);

        Assert.Equal(1, generator.GenerateCalls);
    }

    [Fact]
    public async Task IsAvailableAsync_NoGenerator_ReturnsFalse()
    {
        Assert.False(await Create(generator: null).IsAvailableAsync(CancellationToken.None));
    }
}
