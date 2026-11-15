using System.Text;
using System.Text.Json.Nodes;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playlists;
using OpenMyo.Yoto.Http;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Content;

/// <summary>Playlists on Yoto's <c>/content</c> API.</summary>
internal sealed class YotoPlaylistRepository(IHttpClientFactory httpClientFactory, CardCache cache) : IPlaylistRepository
{
    public async Task<IReadOnlyList<PlaylistSummary>> ListAsync(CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var response = await client.GetAsync("content/mine?showdeleted=false", cancellationToken);
        var list = await YotoResponse.ReadAsync(response, YotoJsonContext.Default.CardListResponse, cancellationToken);
        return (list.Cards ?? []).Select(CardMapper.ToSummary).OfType<PlaylistSummary>().ToList();
    }

    public async Task<Playlist> GetAsync(PlaylistSummary summary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var cached = await cache.GetCurrentAsync(CardIds.RequireCardId(summary.CardId), summary.UpdatedAt, cancellationToken);
        var card = cached ?? await FetchCardAsync(summary.CardId, cancellationToken);
        return CardMapper.ToPlaylist(card);
    }

    public async Task<Playlist> SaveAsync(Playlist playlist, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        JsonObject? raw = null;
        if (playlist.CardId is not null)
        {
            // Save on top of the latest known document so fields this app does not model are kept.
            raw = await cache.GetLastKnownAsync(CardIds.RequireCardId(playlist.CardId), cancellationToken)
                ?? await FetchCardAsync(playlist.CardId, cancellationToken);
        }

        var body = CardComposer.Compose(raw, playlist);
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("content", content, cancellationToken);
        var saved = await YotoResponse.ReadObjectAsync(response, cancellationToken);

        var card = saved["card"] as JsonObject ?? throw new YotoApiException(response.StatusCode, null, "Yoto did not return the saved card.");
        var cardId = CardIds.RequireCardId(card.GetString("cardId") ?? playlist.CardId);
        card = card.GetObject("content") is null ? await FetchCardAsync(cardId, cancellationToken) : card;
        await cache.StoreAsync(cardId, card, cancellationToken);
        return CardMapper.ToPlaylist(card);
    }

    public async Task DeleteAsync(string cardId, CancellationToken cancellationToken)
    {
        CardIds.RequireCardId(cardId);
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var response = await client.DeleteAsync($"content/{cardId}", cancellationToken);
        await YotoResponse.EnsureSuccessAsync(response, cancellationToken);
        cache.Remove(cardId);
    }

    private async Task<JsonObject> FetchCardAsync(string cardId, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var response = await client.GetAsync($"content/{CardIds.RequireCardId(cardId)}", cancellationToken);
        var root = await YotoResponse.ReadObjectAsync(response, cancellationToken);
        var card = root["card"] as JsonObject ?? throw new YotoApiException(response.StatusCode, null, "Yoto did not return the card.");
        await cache.StoreAsync(cardId, card, cancellationToken);
        return card;
    }
}
