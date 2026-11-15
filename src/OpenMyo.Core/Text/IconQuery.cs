using System.Text.RegularExpressions;

namespace OpenMyo.Core.Text;

/// <summary>
/// Turns a track title into what to search icons for: its subject words ("It Began with a Rabbit's Foot" →
/// "Rabbit's Foot"), without track numbers ("[151]", "152a", "01."), bracketed asides, "Part 2", a leading
/// "Bonus!", filler words or words the whole series repeats.
/// </summary>
public static partial class IconQuery
{
    // Includes the modifier-letter colon (U+A789) some stores use in file-safe titles.
    private static readonly char[] Punctuation = [' ', '-', ':', '\uA789', ',', '.', '!', '?', '–', '—'];

    /// <summary>The title's subject words; falls back to the cleaned title when every word is filler.</summary>
    public static string FromTitle(string title) => FromTitle(title, new HashSet<string>());

    /// <summary>As <see cref="FromTitle(string)"/>, also ignoring <paramref name="seriesWords"/> (normalized).</summary>
    public static string FromTitle(string title, IReadOnlySet<string> seriesWords)
    {
        ArgumentNullException.ThrowIfNull(seriesWords);

        var cleaned = Clean(title);
        var subject = cleaned.Split(' ').Where(token => TextNormalizer.Keywords(token)
            .Any(word => !WeakWords.Contains(word) && !seriesWords.Contains(word)));
        var query = string.Join(' ', subject).Trim(Punctuation);
        return query.Length > 0 ? query : cleaned;
    }

    /// <summary>The title without numbering, bracketed asides, "Part 2" or a leading "Bonus!".</summary>
    public static string Clean(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var query = Bracketed().Replace(title, " ");
        query = LeadingNumber().Replace(query, string.Empty);
        query = BonusPrefix().Replace(query, string.Empty);
        query = PartNumber().Replace(query, " ");
        query = RepeatedSpaces().Replace(query, " ").Trim(Punctuation);

        // A title that is only a number or an aside keeps its text rather than searching for nothing.
        return query.Length > 0 ? query : title.Trim();
    }

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)|\{[^}]*\}", RegexOptions.CultureInvariant)]
    private static partial Regex Bracketed();

    [GeneratedRegex(@"^\s*(?:track|chapter|episode|no\.?)?\s*\d+[a-z]?\s*[-.:)–—]?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LeadingNumber();

    [GeneratedRegex(@"^\s*bonus\s*[!:\-–—]\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BonusPrefix();

    [GeneratedRegex(@"[,:\-–—]?\s*\b(?:part|pt\.?|chapter|vol\.?|volume)\s*\d+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PartNumber();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedSpaces();
}
