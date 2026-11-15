using System.Text;
using System.Text.Json;

namespace OpenMyo.Claude;

/// <summary>Builds the prompt and output schema for rewriting track titles.</summary>
internal static class TitlePrompt
{
    private static readonly string[] RootRequired = ["tracks"];
    private static readonly string[] TrackRequired = ["index", "title"];

    /// <summary>Fixed text, so prompt caching can reuse it across playlists.</summary>
    public const string SystemPrompt = """
        You tidy track titles for a children's audio player. Titles often come from file names or ripped CDs and
        look like "03_the_gruffalo_(unabridged)_192kbps" or "Julia Donaldson - Room on the Broom - Part 2".

        For each track, return the clean title a parent would want to see on screen:
        - Keep the real name of the song, story or chapter. Never invent content, translate, or summarise.
        - Keep the leading chapter or track number exactly as written at the start ("[051] Connie",
          "Chapter 3: The Wolf", "03 The Gruffalo"); it is how the listener finds their place.
        - Remove file extensions, bitrates, underscores, "(Official Audio)", disc markers and similar leftovers.
        - Remove text that only repeats the playlist title, album or artist, unless nothing else would remain.
        - Use title case and single spaces. Keep it under 140 characters.
        - If a title is already good, return it unchanged.
        Use the playlist title and neighbouring tracks as context.
        """;

    public static string UserMessage(string playlistTitle, IReadOnlyList<string> trackTitles)
    {
        var builder = new StringBuilder();
        builder.Append("Playlist: ").AppendLine(playlistTitle).AppendLine("Tracks:");
        for (var i = 0; i < trackTitles.Count; i++)
        {
            builder.Append(i).Append(": ").AppendLine(trackTitles[i]);
        }

        return builder.ToString();
    }

    /// <summary>JSON schema: { "tracks": [ { "index": 0, "title": "The Gruffalo" } ] }.</summary>
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
                        title = new { type = "string" },
                    },
                },
            },
        }),
    };
}
