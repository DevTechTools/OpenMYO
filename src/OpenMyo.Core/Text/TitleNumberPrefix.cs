using System.Text.RegularExpressions;

namespace OpenMyo.Core.Text;

/// <summary>
/// Keeps a title's leading chapter number through a rewrite: "[051] Connie", "Chapter 3: The Wolf" and
/// "03_the_gruffalo" keep "[051]", "Chapter 3" and "03" when a rewritten title dropped them.
/// </summary>
public static partial class TitleNumberPrefix
{
    /// <summary>
    /// Returns <paramref name="rewritten"/> with the original's leading number put back in front, unless the
    /// rewritten title already starts with that number or the original had none.
    /// </summary>
    public static string Restore(string original, string rewritten)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(rewritten);
        var match = Prefix().Match(original);
        if (!match.Success)
        {
            return rewritten;
        }

        var number = TitleNumbers.LeadingNumber(match.Value);
        if (number is not null && TitleNumbers.LeadingNumber(rewritten) == number && StartsWithNumber(rewritten))
        {
            return rewritten;
        }

        var prefix = match.Groups["prefix"].Value;
        var separator = char.IsLetter(prefix[0]) ? ": " : " ";
        return prefix + separator + rewritten;
    }

    // The number sits at the start once a word like "Chapter" or a bracket is skipped: "Chapter 3: X", "[051] X", "3. X".
    private static bool StartsWithNumber(string title) => WordedOrBareStart().IsMatch(title);

    [GeneratedRegex(
        @"^\s*(?<prefix>\[\d{1,6}[a-z]?\]|(?:chapter|part|episode|track)\s*\d{1,6}[a-z]?|\d{1,6}[a-z]?)(?=$|[\s_.:)\-–—])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Prefix();

    [GeneratedRegex(
        @"^\s*(?:\[|(?:chapter|part|episode|track)\s*)?\d",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WordedOrBareStart();
}
