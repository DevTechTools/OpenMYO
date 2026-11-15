using System.Text.Json;
using OpenMyo.Core.Icons;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Icons;

internal static class IconMapper
{
    /// <summary>
    /// Parses a Yoto icon list (public or the user's own) into display icons. Skips entries without a media id
    /// and keeps the first of any duplicates.
    /// </summary>
    /// <exception cref="JsonException">The text is not a valid icon list.</exception>
    public static IReadOnlyList<DisplayIcon> ParseIconList(string json, bool isPublic) =>
        (JsonSerializer.Deserialize(json, YotoJsonContext.Default.IconListResponse)?.DisplayIcons ?? [])
            .Where(item => !string.IsNullOrEmpty(item.MediaId))
            .DistinctBy(item => item.MediaId, StringComparer.Ordinal)
            .Select(item => new DisplayIcon(
                item.MediaId!, // Filtered above.
                item.Title ?? string.Empty,
                item.PublicTags ?? [],
                Uri.TryCreate(item.Url, UriKind.Absolute, out var url) ? url : null,
                isPublic))
            .ToList();
}
