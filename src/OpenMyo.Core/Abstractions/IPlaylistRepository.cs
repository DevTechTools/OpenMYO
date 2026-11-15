using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Abstractions;

/// <summary>Reads and writes the signed-in user's MYO playlists.</summary>
public interface IPlaylistRepository
{
    /// <summary>Lists every playlist without chapter details (one fast request).</summary>
    Task<IReadOnlyList<PlaylistSummary>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Loads a playlist with its chapters. Implementations may serve unchanged cards from cache.</summary>
    Task<Playlist> GetAsync(PlaylistSummary summary, CancellationToken cancellationToken);

    /// <summary>Creates the playlist when <see cref="Playlist.CardId"/> is null, otherwise updates it.</summary>
    /// <returns>The saved playlist, including its card id.</returns>
    Task<Playlist> SaveAsync(Playlist playlist, CancellationToken cancellationToken);

    Task DeleteAsync(string cardId, CancellationToken cancellationToken);
}
