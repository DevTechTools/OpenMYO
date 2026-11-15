namespace OpenMyo.Mcp.Playlists;

/// <summary>One row of list_playlists. Track counts are omitted: Yoto's listing does not include them.</summary>
public sealed record PlaylistListItem(string Id, string Title, string? Updated);

/// <summary>A track as get_playlist shows it. <paramref name="Query"/> is a good starting point for search_icons.</summary>
public sealed record TrackView(int Index, string Title, OpenMyo.Mcp.Support.IconRef? Icon, string Query);

/// <summary>get_playlist result.</summary>
public sealed record PlaylistView(string Id, string Title, IReadOnlyList<TrackView> Tracks);
