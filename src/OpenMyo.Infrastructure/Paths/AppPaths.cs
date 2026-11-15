using OpenMyo.Core.Abstractions;

namespace OpenMyo.Infrastructure.Paths;

/// <summary>Resolves per-user data and cache directories following each OS's conventions.</summary>
public sealed class AppPaths : IAppPaths
{
    private const string AppFolderName = "OpenMyo";
    private const string WindowsCacheFolderName = "Cache";

    private readonly Lazy<string> _dataDirectory = new(() => CreateIfMissing(ResolveDataDirectory()));
    private readonly Lazy<string> _cacheDirectory = new(() => CreateIfMissing(ResolveCacheDirectory()));

    public string DataDirectory => _dataDirectory.Value;

    public string CacheDirectory => _cacheDirectory.Value;

    private static string ResolveDataDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    private static string ResolveCacheDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(home, "Library", "Caches", AppFolderName);
        }

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(ResolveDataDirectory(), WindowsCacheFolderName);
        }

        var xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        var root = string.IsNullOrWhiteSpace(xdgCache) ? Path.Combine(home, ".cache") : xdgCache;
        return Path.Combine(root, AppFolderName);
    }

    private static string CreateIfMissing(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
