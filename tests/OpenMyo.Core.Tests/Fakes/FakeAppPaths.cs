using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Tests.Fakes;

/// <summary>App paths rooted in a test folder.</summary>
public sealed class FakeAppPaths(string root) : IAppPaths
{
    public string DataDirectory => root;

    public string CacheDirectory => root;
}
