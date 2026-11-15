using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Finds Yoto's public number icons ("Numbers - 12"), so a track can show its position on the player.
/// Yoto publishes 1–30 at the time of writing, with gaps. Icons the app drew for the missing ones live in the
/// user's account titled <see cref="DrawnTitle"/>; a distinct title means earlier drawings in an older style
/// (titled like Yoto's) are ignored and redrawn. Yoto's own win when both exist.
/// </summary>
public static partial class NumberIcons
{
    private const string DrawnPrefix = "OpenMyo number ";

    /// <summary>The title given to a number icon the app drew and uploaded.</summary>
    public static string DrawnTitle(int number) => DrawnPrefix + number.ToString(CultureInfo.InvariantCulture);

    public static IReadOnlyDictionary<int, DisplayIcon> From(IEnumerable<DisplayIcon> icons)
    {
        ArgumentNullException.ThrowIfNull(icons);

        var numbers = new Dictionary<int, DisplayIcon>();
        foreach (var icon in icons.OrderBy(icon => icon.IsPublic ? 0 : 1))
        {
            var match = (icon.IsPublic ? NumberTitle() : DrawnNumberTitle()).Match(icon.Title);
            if (match.Success)
            {
                numbers.TryAdd(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), icon);
            }
        }

        return numbers;
    }

    [GeneratedRegex(@"^\s*Numbers?\s*-\s*(\d{1,3})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NumberTitle();

    [GeneratedRegex(@"^OpenMyo number (\d{1,3})$", RegexOptions.CultureInvariant)]
    private static partial Regex DrawnNumberTitle();
}
