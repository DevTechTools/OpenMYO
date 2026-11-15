using System.Globalization;

namespace OpenMyo.Core.Library;

/// <summary>Where a search word was found in a piece of text, for highlighting.</summary>
public readonly record struct TextMatch(int Start, int Length);

/// <summary>
/// What the user typed in the library search field, split into words. Text matches when it contains every
/// word, ignoring case and accents, so "gruff child" finds "The Gruffalo's Child" and "ele" finds "Élé".
/// </summary>
public sealed class SearchQuery
{
    private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
    private static readonly CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;

    private readonly string[] _words;

    private SearchQuery(string[] words) => _words = words;

    /// <summary>True when nothing was typed; an empty query matches everything.</summary>
    public bool IsEmpty => _words.Length == 0;

    public static SearchQuery Parse(string? text) =>
        new((text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Returns the highlighted ranges when <paramref name="text"/> contains every word, otherwise null.</summary>
    public IReadOnlyList<TextMatch>? Match(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var ranges = new List<TextMatch>(_words.Length);
        foreach (var word in _words)
        {
            var start = Compare.IndexOf(text.AsSpan(), word.AsSpan(), Options, out var length);
            if (start < 0)
            {
                return null;
            }

            ranges.Add(new TextMatch(start, length));
        }

        return Merge(ranges);
    }

    /// <summary>Sorts ranges and joins overlapping ones, so each character is highlighted once.</summary>
    private static List<TextMatch> Merge(List<TextMatch> ranges)
    {
        ranges.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<TextMatch>(ranges.Count);
        foreach (var range in ranges)
        {
            if (merged.Count > 0 && range.Start <= merged[^1].Start + merged[^1].Length)
            {
                var last = merged[^1];
                var end = Math.Max(last.Start + last.Length, range.Start + range.Length);
                merged[^1] = last with { Length = end - last.Start };
                continue;
            }

            merged.Add(range);
        }

        return merged;
    }
}
