using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Icons;

/// <summary>set_track_icons / number_track_icons result. <paramref name="Previous"/> lets Claude undo the change.</summary>
public sealed record SetIconsResult(int Saved, IReadOnlyList<PreviousIcon> Previous);
