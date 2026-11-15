using System.Globalization;
using System.Text;

namespace OpenMyo.Core.Text;

/// <summary>Turns free text into lower-case ASCII-ish words suitable for fuzzy matching.</summary>
public static class TextNormalizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "the", "of", "to", "in", "on", "for", "with", "at", "by", "from",
        "is", "it", "its", "my", "your", "our", "me", "you", "we", "be", "are", "was",
        "part", "chapter", "track", "song", "episode", "vol", "volume",
    };

    /// <summary>Lower-cases, strips accents and replaces anything that is not a letter or digit with a space.</summary>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return builder.ToString();
    }

    /// <summary>Splits normalized text into words, dropping stop words and single characters.</summary>
    public static IReadOnlyList<string> Keywords(string text) =>
        Normalize(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 1 && !StopWords.Contains(word) && !IsNumber(word))
            .Select(Singularize)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>Very small English plural stemmer: "stars" → "star", "foxes" → "fox", "berries" → "berry".</summary>
    public static string Singularize(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal))
        {
            return word[..^3] + "y";
        }

        if (word.Length > 4 && (word.EndsWith("xes", StringComparison.Ordinal) || word.EndsWith("ches", StringComparison.Ordinal)))
        {
            return word[..^2];
        }

        return word.Length > 3 && word[^1] == 's' && word[^2] != 's' ? word[..^1] : word;
    }

    // "151" and episode ids like "152a" are numbering, never something to picture.
    private static bool IsNumber(string word) =>
        word.All(char.IsDigit) || (word.Length > 1 && char.IsLetter(word[^1]) && word[..^1].All(char.IsDigit));
}
