using System.Text.Json.Serialization;

namespace OpenMyo.Mcp.Icons;

/// <summary>One search hit. <paramref name="Id"/> goes straight into set_track_icons.</summary>
public sealed record IconHit(
    string Id,
    string Title,
    string Source,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Artist);

/// <summary>search_icons result; <paramref name="Note"/> explains anything left out.</summary>
public sealed record IconSearchResult(
    IReadOnlyList<IconHit> Icons,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Note);
