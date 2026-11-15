using System.Text.Json.Serialization;

namespace OpenMyo.Mcp.Support;

/// <summary>An icon as Claude sees it: its id plus a human title when the index knows one.</summary>
public sealed record IconRef(string Id, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Title);
