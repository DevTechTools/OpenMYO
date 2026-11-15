namespace OpenMyo.Core.Tests.Fakes;

/// <summary>A unique folder under the system temp path that is deleted on dispose.</summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "openmyo-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>Creates a file (and any parent folders) and returns its full path.</summary>
    public string CreateFile(string relativePath, byte[]? content = null)
    {
        var fullPath = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, content ?? []);
        return fullPath;
    }

    /// <summary>
    /// Joins <paramref name="relativePath"/> (written with '/') to the folder, normalized to the platform's
    /// separators so it matches paths returned by <see cref="Directory.EnumerateFiles(string)"/> on Windows.
    /// </summary>
    public string Combine(string relativePath) => System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, relativePath));

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
