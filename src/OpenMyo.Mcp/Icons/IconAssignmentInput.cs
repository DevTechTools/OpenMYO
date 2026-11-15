using System.ComponentModel;

namespace OpenMyo.Mcp.Icons;

/// <summary>One requested change for set_track_icons.</summary>
public sealed record IconAssignmentInput(
    [property: Description("1-based track number.")] int Track,
    [property: Description("Icon id from search_icons (a 'yotoicons:' id is imported first).")] string Icon);
