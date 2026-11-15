using System.Text.Json.Serialization;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Icons;

/// <summary>A track whose icon auto_match_icons changed; <paramref name="Was"/> is the old icon id, if any.</summary>
public sealed record MatchedTrack(
    int Track,
    string Title,
    IconRef Icon,
    string Source,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Was);

/// <summary>A track left without an icon, with the query to start searching from.</summary>
public sealed record MissingTrack(int Track, string Title, string Query);

/// <summary>auto_match_icons result.</summary>
public sealed record AutoMatchResult(IReadOnlyList<MatchedTrack> Changed, IReadOnlyList<MissingTrack> StillMissing);
