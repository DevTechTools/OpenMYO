using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenMyo.Claude;

/// <summary>Parses Claude's structured output into keywords per track index.</summary>
internal static class KeywordResponse
{
    public static IReadOnlyList<IReadOnlyList<string>> Parse(string json, int trackCount)
    {
        var payload = JsonSerializer.Deserialize(json, KeywordJsonContext.Default.KeywordPayload)
            ?? throw new JsonException("Claude returned an empty keyword payload.");

        var result = Enumerable.Range(0, trackCount).Select(_ => (IReadOnlyList<string>)[]).ToArray();
        foreach (var track in payload.Tracks.Where(track => track.Index >= 0 && track.Index < trackCount))
        {
            result[track.Index] = track.Keywords
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Select(keyword => keyword.Trim())
                .Take(3)
                .ToList();
        }

        return result;
    }
}

internal sealed record KeywordPayload(IReadOnlyList<TrackKeywords> Tracks);

internal sealed record TrackKeywords(int Index, IReadOnlyList<string> Keywords);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(KeywordPayload))]
internal sealed partial class KeywordJsonContext : JsonSerializerContext;
