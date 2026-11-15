using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenMyo.Claude;

/// <summary>Parses Claude's structured output into one title (or null) per track index.</summary>
internal static class TitleResponse
{
    public static IReadOnlyList<string?> Parse(string json, int trackCount)
    {
        var payload = JsonSerializer.Deserialize(json, TitleJsonContext.Default.TitlePayload)
            ?? throw new JsonException("Claude returned an empty title payload.");

        var result = new string?[trackCount];
        foreach (var track in payload.Tracks.Where(track => track.Index >= 0 && track.Index < trackCount))
        {
            result[track.Index] = string.IsNullOrWhiteSpace(track.Title) ? null : track.Title.Trim();
        }

        return result;
    }
}

internal sealed record TitlePayload(IReadOnlyList<TrackTitle> Tracks);

internal sealed record TrackTitle(int Index, string? Title);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TitlePayload))]
internal sealed partial class TitleJsonContext : JsonSerializerContext;
