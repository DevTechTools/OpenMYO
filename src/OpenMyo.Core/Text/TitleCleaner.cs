using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenMyo.Core.Text;

/// <summary>
/// Turns messy file names and tags into display titles:
/// "03_-_twinkle_twinkle_(Official Audio) [x7Gh2]" → "Twinkle Twinkle".
/// </summary>
public static partial class TitleCleaner
{
    /// <summary>Yoto rejects card and chapter titles longer than this.</summary>
    public const int MaxTitleLength = 140;

    /// <summary>Cleans one title. Bare leading numbers ("01 Song") are removed only when <paramref name="stripBareNumber"/> is set.</summary>
    public static string Clean(string raw, bool stripBareNumber = false) => Clean(raw, TitleCleanupOptions.Default, stripBareNumber);

    /// <summary>Cleans one title with only the rules switched on in <paramref name="options"/>.</summary>
    public static string Clean(string raw, TitleCleanupOptions options, bool stripBareNumber = false)
    {
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentNullException.ThrowIfNull(options);

        var text = options.FixSeparators ? WebAddress().Replace(SeparatorsToSpaces(raw), " ") : raw;
        text = options.RemoveNoise ? NoiseInBrackets().Replace(text, " ") : text;
        text = options.RemoveBracketedText ? SquareBrackets().Replace(text, " ") : text;
        text = options.RemoveNumbering ? RemoveNumbering(text, stripBareNumber) : text;
        text = Whitespace().Replace(text, " ").Trim(' ', '-', '–', '—', '_', '.', ',');
        text = options.FixCapitals ? FixShouting(text) : text;
        text = ApplyFindReplace(text, options);
        return Truncate(text.Length == 0 ? raw.Trim() : text);
    }

    /// <summary>
    /// Cleans a whole playlist together so patterns shared by every title can be removed,
    /// e.g. a repeated "Artist - " prefix or consistent "01 " numbering.
    /// </summary>
    public static IReadOnlyList<string> CleanAll(IReadOnlyList<string> raws) => CleanAll(raws, TitleCleanupOptions.Default);

    /// <inheritdoc cref="CleanAll(IReadOnlyList{string})"/>
    public static IReadOnlyList<string> CleanAll(IReadOnlyList<string> raws, TitleCleanupOptions options)
    {
        ArgumentNullException.ThrowIfNull(raws);
        ArgumentNullException.ThrowIfNull(options);

        var allNumbered = raws.Count > 1 && raws.All(raw => raw.Length > 0 && char.IsDigit(raw.TrimStart()[0]));
        var cleaned = raws.Select(raw => Clean(raw, options, stripBareNumber: allNumbered)).ToList();
        return options.RemoveSharedPrefix ? RemoveSharedPrefix(cleaned) : cleaned;
    }

    private static string RemoveNumbering(string text, bool stripBareNumber)
    {
        text = DiscTrackPrefix().Replace(text, string.Empty);
        text = PunctuatedNumberPrefix().Replace(text, string.Empty);
        // "01 Song" is numbering even on its own; "99 Red Balloons" is only stripped when the whole playlist is numbered.
        return stripBareNumber || ZeroPaddedPrefix().IsMatch(text) ? BareNumberPrefix().Replace(text, string.Empty) : text;
    }

    private static string ApplyFindReplace(string text, TitleCleanupOptions options) =>
        options.Find.Length == 0
            ? text
            : Whitespace().Replace(text.Replace(options.Find, options.Replace, StringComparison.OrdinalIgnoreCase), " ").Trim();

    private static List<string> RemoveSharedPrefix(List<string> titles)
    {
        const string separator = " - ";
        if (titles.Count < 2 || !titles.All(title => title.Contains(separator, StringComparison.Ordinal)))
        {
            return titles;
        }

        var prefix = titles[0][..(titles[0].IndexOf(separator, StringComparison.Ordinal) + separator.Length)];
        var allShare = titles.All(title => title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && title.Length > prefix.Length);
        return allShare ? titles.Select(title => title[prefix.Length..].Trim()).ToList() : titles;
    }

    private static string SeparatorsToSpaces(string raw)
    {
        var text = raw.Replace('_', ' ').Replace("%20", " ", StringComparison.Ordinal);
        // "my.song.name" uses dots as word separators; "Mr. Brown" does not.
        return !text.Contains(' ', StringComparison.Ordinal) && text.Count(c => c == '.') > 1 ? text.Replace('.', ' ') : text;
    }

    private static string FixShouting(string text)
    {
        var letters = text.Where(char.IsLetter).ToList();
        var isSingleCase = letters.Count > 3 && (letters.All(char.IsUpper) || letters.All(char.IsLower));
        return isSingleCase ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()) : text;
    }

    private static string Truncate(string text) =>
        text.Length <= MaxTitleLength ? text : text[..MaxTitleLength].TrimEnd();

    [GeneratedRegex(@"[\(\[\{][^\)\]\}]*\b(official|audio|video|lyrics?|remaster(ed)?|\d+\s?kbps|hq|hd|explicit|clean|mp3|visuali[sz]er)\b[^\)\]\}]*[\)\]\}]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoiseInBrackets();

    [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.CultureInvariant)]
    private static partial Regex SquareBrackets();

    [GeneratedRegex(@"\b(https?://)?(www\.)?[a-z0-9-]+\.(com|net|org|co\.uk|io)\b\S*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WebAddress();

    [GeneratedRegex(@"^\s*(?:(?:cd|dis[ck])\s*\d{1,2}\s*[-–.]?\s*|\d{1,2}-\d{1,3}\s+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiscTrackPrefix();

    [GeneratedRegex(@"^\s*(?:track\s*)?\d{1,3}\s*[-–—.):]\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PunctuatedNumberPrefix();

    [GeneratedRegex(@"^\s*(?:track\s*)?\d{1,3}\s+(?=\S)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BareNumberPrefix();

    [GeneratedRegex(@"^\s*0\d{1,2}\s+\S", RegexOptions.CultureInvariant)]
    private static partial Regex ZeroPaddedPrefix();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
