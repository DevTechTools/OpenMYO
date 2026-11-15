namespace OpenMyo.Core.Text;

/// <summary>Reads the word lists shipped inside OpenMyo.Core, skipping blank lines and "#" comments.</summary>
internal static class EmbeddedLines
{
    public static IEnumerable<string> Read(string resourceName)
    {
        using var stream = typeof(EmbeddedLines).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded resource {resourceName} is missing from OpenMyo.Core.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith('#'))
            .ToList();
    }
}
