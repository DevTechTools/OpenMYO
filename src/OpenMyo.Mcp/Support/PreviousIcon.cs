using System.Text.Json.Serialization;

namespace OpenMyo.Mcp.Support;

/// <summary>A track's icon before a change (null id: it had none); pass the id back to set_track_icons to undo.</summary>
public sealed record PreviousIcon(
    int Track,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Icon,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Title);
