using OpenMyo.Core.Abstractions;

namespace OpenMyo.Yoto.Tests.Support;

internal sealed class StubHttpClientFactory(HttpMessageHandler api, HttpMessageHandler? upload = null) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = name == "yoto-upload" ? upload ?? api : api;
        return new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.yotoplay.com/") };
    }
}

internal sealed class TempPaths : IAppPaths
{
    public TempPaths()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "openmyo-tests", Guid.NewGuid().ToString("N"));
        CacheDirectory = Path.Combine(DataDirectory, "cache");
    }

    public string DataDirectory { get; }

    public string CacheDirectory { get; }
}
