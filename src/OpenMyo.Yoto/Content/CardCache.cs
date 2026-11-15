using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenMyo.Core.Abstractions;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Content;

/// <summary>
/// Last-fetched raw card documents, in memory and on disk. A cached card is reusable while its
/// <c>updatedAt</c> equals the one in the library listing, so unchanged cards need no network call.
/// </summary>
internal sealed class CardCache(IAppPaths paths)
{
    private readonly ConcurrentDictionary<string, JsonObject> _memory = new(StringComparer.Ordinal);

    /// <summary>Returns the cached card only when it is still current for <paramref name="updatedAt"/>.</summary>
    public async Task<JsonObject?> GetCurrentAsync(string cardId, DateTimeOffset? updatedAt, CancellationToken cancellationToken)
    {
        if (updatedAt is null)
        {
            return null;
        }

        var card = await GetLastKnownAsync(cardId, cancellationToken);
        return card is not null && CardMapper.ParseDate(card.GetString("updatedAt")) == updatedAt ? card : null;
    }

    /// <summary>Returns the last known version regardless of age; used as the base for saving.</summary>
    public async Task<JsonObject?> GetLastKnownAsync(string cardId, CancellationToken cancellationToken)
    {
        if (_memory.TryGetValue(cardId, out var card))
        {
            return card;
        }

        card = await ReadFromDiskAsync(cardId, cancellationToken);
        if (card is not null)
        {
            _memory[cardId] = card;
        }

        return card;
    }

    public async Task StoreAsync(string cardId, JsonObject card, CancellationToken cancellationToken)
    {
        _memory[cardId] = card;
        try
        {
            var path = PathFor(cardId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!); // PathFor always includes a directory.
            var temp = path + ".tmp";
            await File.WriteAllTextAsync(temp, card.ToJsonString(), cancellationToken);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The disk cache is an optimisation; the in-memory copy still works.
        }
    }

    public void Remove(string cardId)
    {
        _memory.TryRemove(cardId, out _);
        try
        {
            File.Delete(PathFor(cardId));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A stale file is harmless: it is only used when updatedAt matches.
        }
    }

    private async Task<JsonObject?> ReadFromDiskAsync(string cardId, CancellationToken cancellationToken)
    {
        try
        {
            var path = PathFor(cardId);
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken)) as JsonObject;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private string PathFor(string cardId) =>
        Path.Combine(paths.CacheDirectory, "cards", CardIds.RequireCardId(cardId) + ".json");
}
