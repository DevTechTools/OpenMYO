using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenMyo.Core.Text;

/// <summary>Reads the number a title is ordered by: "[052a] Bonus" → 52, "Episode 7" → 7, "Intro" → null.</summary>
public static partial class TitleNumbers
{
    public static int? LeadingNumber(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        var match = FirstNumber().Match(title);
        return match.Success && int.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    /// <summary>True when the numbered titles are not in ascending order. Unnumbered titles are ignored.</summary>
    public static bool IsOutOfOrder(IReadOnlyList<string> titles)
    {
        ArgumentNullException.ThrowIfNull(titles);
        var numbers = titles.Select(LeadingNumber).OfType<int>().ToList();
        return numbers.Count * 2 >= titles.Count && numbers.Zip(numbers.Skip(1)).Any(pair => pair.Second < pair.First);
    }

    /// <summary>
    /// Orders by number, keeping the current order among equal numbers ("[101]" stays before "[101a] Bonus").
    /// Unnumbered titles keep their place at the end.
    /// </summary>
    public static IReadOnlyList<T> SortByNumber<T>(IReadOnlyList<T> items, Func<T, string> title) =>
        items.OrderBy(item => LeadingNumber(title(item)) ?? int.MaxValue).ToList();

    [GeneratedRegex(@"\d{1,6}", RegexOptions.CultureInvariant)]
    private static partial Regex FirstNumber();
}
