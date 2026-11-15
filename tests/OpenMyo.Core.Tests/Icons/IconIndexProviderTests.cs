using OpenMyo.Core.Icons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Icons;

public class IconIndexProviderTests
{
    [Fact]
    public async Task GetAsync_CalledTwice_BuildsOnce()
    {
        var catalog = new FakeIconCatalog([TestIcons.Make("cat", "cat")]);
        var provider = new IconIndexProvider(catalog);

        var first = await provider.GetAsync(CancellationToken.None);
        var second = await provider.GetAsync(CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, catalog.CallCount);
    }

    [Fact]
    public async Task GetAsync_AfterInvalidate_Rebuilds()
    {
        var catalog = new FakeIconCatalog([TestIcons.Make("cat", "cat")]);
        var provider = new IconIndexProvider(catalog);
        await provider.GetAsync(CancellationToken.None);

        provider.Invalidate();
        await provider.GetAsync(CancellationToken.None);

        Assert.Equal(2, catalog.CallCount);
    }
}
