using ModelContextProtocol.Protocol;

namespace OpenMyo.Mcp.Support;

/// <summary>The OpenMyo logo as data URIs, so MCP clients can show it without fetching anything.</summary>
public static class ServerIcons
{
    // Must match the LogicalName of the EmbeddedResource items in OpenMyo.Mcp.csproj.
    private const string ResourcePrefix = "OpenMyo.Mcp.";
    private const string PngMimeType = "image/png";
    private const string SvgMimeType = "image/svg+xml";

    /// <summary>PNG (which every icon-capable client supports) first, then the scalable SVG.</summary>
    public static IList<Icon> Load() =>
    [
        CreateIcon("openmyo-icon.png", PngMimeType, "256x256"),
        CreateIcon("openmyo-icon.svg", SvgMimeType, "any"),
    ];

    private static Icon CreateIcon(string name, string mimeType, string size) => new()
    {
        Source = $"data:{mimeType};base64,{Convert.ToBase64String(ReadResource(name))}",
        MimeType = mimeType,
        Sizes = [size],
    };

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(ServerIcons).Assembly.GetManifestResourceStream(ResourcePrefix + name)
            ?? throw new InvalidOperationException($"Missing embedded icon '{name}'.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
