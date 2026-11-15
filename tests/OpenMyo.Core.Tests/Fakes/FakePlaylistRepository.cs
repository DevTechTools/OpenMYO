using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Fakes;

public sealed class FakePlaylistRepository : IPlaylistRepository
{
    public List<Playlist> Saved { get; } = [];

    public Task<Playlist> SaveAsync(Playlist playlist, CancellationToken cancellationToken)
    {
        var saved = playlist with { CardId = playlist.CardId ?? "new-card" };
        Saved.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<IReadOnlyList<PlaylistSummary>> ListAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Playlist> GetAsync(PlaylistSummary summary, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task DeleteAsync(string cardId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
