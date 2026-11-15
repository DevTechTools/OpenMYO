using ModelContextProtocol;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Mcp.Support;

/// <summary>Finds and saves playlists for tools, turning an unknown id into advice Claude can follow.</summary>
public sealed class PlaylistLookup(IPlaylistRepository repository)
{
    /// <summary>Loads the playlist whose card id is <paramref name="id"/>.</summary>
    /// <exception cref="McpException">No playlist has that id.</exception>
    public async Task<Playlist> LoadAsync(string id, CancellationToken cancellationToken)
    {
        var summaries = await repository.ListAsync(cancellationToken);
        var summary = summaries.FirstOrDefault(item => string.Equals(item.CardId, id, StringComparison.Ordinal))
            ?? throw new McpException($"No playlist with id '{id}'. Call list_playlists to see the ids.");
        return await repository.GetAsync(summary, cancellationToken);
    }

    public Task<Playlist> SaveAsync(Playlist playlist, CancellationToken cancellationToken) =>
        repository.SaveAsync(playlist, cancellationToken);
}
