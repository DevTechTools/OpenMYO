namespace OpenMyo.Core.Text;

/// <summary>
/// Drawable words related to title words that icons rarely use ("trivia" → question, quiz), so a title can
/// match an icon tagged with a related word. Loaded once from the embedded Synonyms.txt.
/// </summary>
public static class WordSynonyms
{
    private const string ResourceName = "OpenMyo.Core.Text.Synonyms.txt";

    private static readonly Dictionary<string, string[]> Map = Load();

    /// <summary>Related words for a normalized, singular keyword, best first; empty when there are none.</summary>
    public static IReadOnlyList<string> For(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        return Map.TryGetValue(word, out var synonyms) ? synonyms : [];
    }

    /// <summary>Parses "head: one, two" lines; blank lines and "#" comments are skipped.</summary>
    internal static Dictionary<string, string[]> Parse(IEnumerable<string> lines)
    {
        var map = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (line.StartsWith('#') || colon <= 0)
            {
                continue;
            }

            var synonyms = line[(colon + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            map[line[..colon].Trim()] = synonyms;
        }

        return map;
    }

    private static Dictionary<string, string[]> Load() => Parse(EmbeddedLines.Read(ResourceName));
}
