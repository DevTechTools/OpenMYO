using System.Text.Json.Nodes;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playback;
using OpenMyo.Yoto.Http;

namespace OpenMyo.Yoto.Content;

/// <summary>
/// Asks Yoto for a card with signed, playable track URLs (<c>playable=true&amp;signingType=s3</c>) instead of the
/// <c>yoto:#</c> references it normally returns. The signed card is never cached: its URLs expire.
/// </summary>
internal sealed class YotoTrackStreams(IHttpClientFactory httpClientFactory) : ITrackStreamSource
{
    public async Task<IReadOnlyDictionary<TrackAddress, Uri>> GetStreamsAsync(string cardId, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        var path = $"content/{CardIds.RequireCardId(cardId)}?playable=true&signingType=s3";
        using var response = await client.GetAsync(path, cancellationToken);
        var root = await YotoResponse.ReadObjectAsync(response, cancellationToken);
        var card = root["card"] as JsonObject
            ?? throw new YotoApiException(response.StatusCode, null, "Yoto did not return the card.");

        var streams = new Dictionary<TrackAddress, Uri>();
        foreach (var chapter in CardMapper.ToPlaylist(card).Chapters)
        {
            foreach (var track in chapter.Tracks)
            {
                if (ToStreamUri(track.TrackUrl) is { } uri)
                {
                    streams[new TrackAddress(chapter.Key, track.Key)] = uri;
                }
            }
        }

        return streams;
    }

    // Anything still a yoto:# reference (or junk) cannot be streamed; only web URLs can.
    private static Uri? ToStreamUri(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri
            : null;
}
