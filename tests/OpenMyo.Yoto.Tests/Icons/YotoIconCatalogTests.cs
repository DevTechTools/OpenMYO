using OpenMyo.Yoto.Icons;
using OpenMyo.Yoto.Tests.Support;

namespace OpenMyo.Yoto.Tests.Icons;

public sealed class YotoIconCatalogTests
{
    private const string PublicJson = """
        {"displayIcons":[
          {"displayIconId":"1","mediaId":"pub1","title":"Cat","publicTags":["pet","cat"],"url":"https://img/1.png"},
          {"displayIconId":"2","mediaId":"pub1","title":"Dup"},
          {"displayIconId":"3","title":"No media id"},
          {"displayIconId":"4","mediaId":"pub2"}]}
        """;

    private const string UserJson = """{"displayIcons":[{"displayIconId":"9","mediaId":"mine1","url":"https://img/9.png"}]}""";

    private static (YotoIconCatalog Catalog, StubHttpHandler Handler) Create()
    {
        var handler = new StubHttpHandler()
            .On("GET /media/displayIcons/user/yoto", PublicJson)
            .On("GET /media/displayIcons/user/me", UserJson);
        return (new YotoIconCatalog(new StubHttpClientFactory(handler), new TempPaths()), handler);
    }

    [Fact]
    public async Task GetIconsAsync_PublicAndUser_MapsAndDedupes()
    {
        var (catalog, _) = Create();

        var icons = await catalog.GetIconsAsync(CancellationToken.None);

        Assert.Equal(["pub1", "pub2", "mine1"], icons.Select(icon => icon.MediaId));
        var cat = icons[0];
        Assert.Equal("Cat", cat.Title);
        Assert.Equal(["pet", "cat"], cat.Tags);
        Assert.True(cat.IsPublic);
        Assert.Equal(string.Empty, icons[1].Title);
        Assert.Empty(icons[1].Tags);
        Assert.False(icons[2].IsPublic);
    }

    [Fact]
    public async Task GetIconsAsync_ConcurrentCalls_ShareOneLoad()
    {
        var (catalog, handler) = Create();

        await Task.WhenAll(catalog.GetIconsAsync(CancellationToken.None), catalog.GetIconsAsync(CancellationToken.None));

        Assert.Single(handler.Requests, request => request.Contains("/yoto", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvalidateUserIcons_AfterLoad_ReloadsOnlyUserIcons()
    {
        var (catalog, handler) = Create();
        await catalog.GetIconsAsync(CancellationToken.None);

        catalog.InvalidateUserIcons();
        await catalog.GetIconsAsync(CancellationToken.None);

        Assert.Equal(1, handler.Requests.Count(r => r.EndsWith("/yoto", StringComparison.Ordinal)));
        Assert.Equal(2, handler.Requests.Count(r => r.EndsWith("/me", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GetIconsAsync_FreshDiskCache_SkipsPublicRequest()
    {
        var paths = new TempPaths();
        var first = new StubHttpHandler().On("GET /media/displayIcons/user/yoto", PublicJson).On("GET /media/displayIcons/user/me", UserJson);
        await new YotoIconCatalog(new StubHttpClientFactory(first), paths).GetIconsAsync(CancellationToken.None);
        var second = new StubHttpHandler().On("GET /media/displayIcons/user/me", UserJson);

        var icons = await new YotoIconCatalog(new StubHttpClientFactory(second), paths).GetIconsAsync(CancellationToken.None);

        Assert.Equal(3, icons.Count);
        Assert.DoesNotContain(second.Requests, r => r.EndsWith("/yoto", StringComparison.Ordinal));
    }
}
