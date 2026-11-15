using System.Text;
using System.Text.Json;
using OpenMyo.Core.Icons;

namespace OpenMyo.Claude;

/// <summary>Builds the prompt and output schema for icon keyword generation.</summary>
internal static class KeywordPrompt
{
    private static readonly string[] RootRequired = ["tracks"];
    private static readonly string[] TrackRequired = ["index", "keywords"];

    private const string Instructions = """
        You choose pictures for tracks on a children's audio player. The player shows a tiny 16x16
        pixel icon for each track, picked by searching an icon library with keywords.

        For each track title, give 1 to 4 keywords naming concrete, drawable things that best represent
        the track (animals, objects, places, weather, vehicles, food, characters' species), most relevant first.
        Prefer words from the vocabulary below; they are the words the icon library is tagged with.
        Use single nouns ("owl", not "the wise owl"). Avoid abstract words ("love", "adventure") unless
        nothing concrete fits. Use the playlist title as context for ambiguous tracks.

        Icon library vocabulary:
        """;

    /// <summary>Byte-identical across calls (the index sorts and caps its vocabulary) so prompt caching reuses it.</summary>
    public static string SystemPrompt(IReadOnlyList<string> vocabulary) =>
        Instructions + "\n" + string.Join(", ", vocabulary);

    public static string UserMessage(IconSuggestionRequest request)
    {
        var builder = new StringBuilder();
        builder.Append("Playlist: ").AppendLine(request.PlaylistTitle).AppendLine("Tracks:");
        for (var i = 0; i < request.TrackTitles.Count; i++)
        {
            builder.Append(i).Append(": ").AppendLine(request.TrackTitles[i]);
        }

        return builder.ToString();
    }

    /// <summary>JSON schema: { "tracks": [ { "index": 0, "keywords": ["owl"] } ] }.</summary>
    public static Dictionary<string, JsonElement> Schema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        ["required"] = JsonSerializer.SerializeToElement(RootRequired),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            tracks = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    required = TrackRequired,
                    properties = new
                    {
                        index = new { type = "integer" },
                        keywords = new { type = "array", items = new { type = "string" } },
                    },
                },
            },
        }),
    };
}
