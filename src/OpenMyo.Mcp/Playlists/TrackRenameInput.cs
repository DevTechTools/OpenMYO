using System.ComponentModel;

namespace OpenMyo.Mcp.Playlists;

/// <summary>One requested change for rename_tracks.</summary>
public sealed record TrackRenameInput(
    [property: Description("1-based track number.")] int Track,
    [property: Description("The new title.")] string Title);
