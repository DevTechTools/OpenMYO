using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Text;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Playlists;

/// <summary>Read-only tools for finding a playlist and seeing its tracks and icons.</summary>
[McpServerToolType]
public sealed class PlaylistTools(IPlaylistRepository repository, PlaylistLookup lookup, IconLabeler labeler)
{
    private const string DateFormat = "yyyy-MM-dd";

    [McpServerTool(Name = "list_playlists", ReadOnly = true)]
    [Description("Lists the user's Yoto Make Your Own playlists as {id, title, updated}. Pass search to keep only titles containing that text (case-insensitive). Use the id with the other tools.")]
    public Task<IReadOnlyList<PlaylistListItem>> ListPlaylists(
        [Description("Optional text the title must contain.")] string? search = null,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var summaries = await repository.ListAsync(cancellationToken);
            return (IReadOnlyList<PlaylistListItem>)summaries
                .Where(item => string.IsNullOrWhiteSpace(search) || item.Title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(item => new PlaylistListItem(item.CardId, item.Title, item.UpdatedAt?.ToString(DateFormat, CultureInfo.InvariantCulture)))
                .ToList();
        });

    [McpServerTool(Name = "get_playlist", ReadOnly = true)]
    [Description("Shows a playlist's tracks: {index (1-based), title, icon {id,title} or null, query}. 'query' is the track title reduced to its subject words; use it as the starting search text for search_icons.")]
    public Task<PlaylistView> GetPlaylist(
        [Description("Playlist id from list_playlists.")] string id,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var playlist = await lookup.LoadAsync(id, cancellationToken);
            var series = SeriesWords.Find(playlist.Chapters.Select(chapter => chapter.Title).ToList());
            var tracks = new List<TrackView>();
            for (var i = 0; i < playlist.Chapters.Count; i++)
            {
                var chapter = playlist.Chapters[i];
                var icon = await labeler.DescribeIconAsync(chapter.IconMediaId, cancellationToken);
                tracks.Add(new TrackView(i + 1, chapter.Title, icon, IconQuery.FromTitle(chapter.Title, series)));
            }

            return new PlaylistView(id, playlist.Title, tracks);
        });
}
