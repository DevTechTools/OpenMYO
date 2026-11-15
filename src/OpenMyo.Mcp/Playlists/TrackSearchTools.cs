using System.Collections.Concurrent;
using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Library;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Playlists;

/// <summary>One track found by search_tracks.</summary>
public sealed record TrackSearchHit(string PlaylistId, string PlaylistTitle, int TrackIndex, string Title);

/// <summary>Finds tracks by title across every playlist, the same way the app's library search does.</summary>
[McpServerToolType]
public sealed class TrackSearchTools(IPlaylistRepository repository)
{
    // Unchanged cards come from the local cache, so loading every playlist is quick after the first time.
    private const int LoadParallelism = 6;
    private const int MaxHits = 50;

    [McpServerTool(Name = "search_tracks", ReadOnly = true)]
    [Description("Finds tracks whose title contains every word of query (ignoring case and accents) across all playlists. Returns up to 50 hits as {playlistId, playlistTitle, trackIndex (1-based), title}. Use playlistId and trackIndex with get_playlist, rename_tracks or play_track.")]
    public Task<IReadOnlyList<TrackSearchHit>> SearchTracks(
        [Description("Words to look for in track titles, e.g. 'gruffalo child'.")] string query,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var search = SearchQuery.Parse(query);
            if (search.IsEmpty)
            {
                throw new McpException("Give at least one word to search for.");
            }

            var hits = new ConcurrentBag<TrackSearchHit>();
            var summaries = await repository.ListAsync(cancellationToken);
            var options = new ParallelOptions { MaxDegreeOfParallelism = LoadParallelism, CancellationToken = cancellationToken };
            await Parallel.ForEachAsync(summaries, options, async (summary, token) =>
            {
                var playlist = await repository.GetAsync(summary, token);
                foreach (var track in TrackSearch.Match(search, string.Empty, playlist)?.Tracks ?? [])
                {
                    hits.Add(new TrackSearchHit(summary.CardId, playlist.Title, track.Number, track.Title));
                }
            });

            return (IReadOnlyList<TrackSearchHit>)hits
                .OrderBy(hit => hit.PlaylistTitle, StringComparer.OrdinalIgnoreCase)
                .ThenBy(hit => hit.TrackIndex)
                .Take(MaxHits)
                .ToList();
        });
}
