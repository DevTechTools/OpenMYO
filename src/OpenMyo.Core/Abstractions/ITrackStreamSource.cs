using OpenMyo.Core.Playback;

namespace OpenMyo.Core.Abstractions;

/// <summary>Looks up playable URLs for the tracks on a card. The URLs are signed and expire, so never cache them.</summary>
public interface ITrackStreamSource
{
    Task<IReadOnlyDictionary<TrackAddress, Uri>> GetStreamsAsync(string cardId, CancellationToken cancellationToken);
}
