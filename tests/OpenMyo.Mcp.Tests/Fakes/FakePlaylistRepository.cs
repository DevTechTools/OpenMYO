using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Mcp.Tests.Fakes;

public sealed class FakePlaylistRepository(params Playlist[] playlists) : IPlaylistRepository
{
    public List<Playlist> Saved { get; } = [];

    public Task<IReadOnlyList<PlaylistSummary>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PlaylistSummary>>(
            playlists.Select(item => new PlaylistSummary(item.CardId!, item.Title, item.UpdatedAt, null)).ToList());

    public Task<Playlist> GetAsync(PlaylistSummary summary, CancellationToken cancellationToken) =>
        Task.FromResult(playlists.Single(item => item.CardId == summary.CardId));

    public Task<Playlist> SaveAsync(Playlist playlist, CancellationToken cancellationToken)
    {
        Saved.Add(playlist);
        return Task.FromResult(playlist);
    }

    public Task DeleteAsync(string cardId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
