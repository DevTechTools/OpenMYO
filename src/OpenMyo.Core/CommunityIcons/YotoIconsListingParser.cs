using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// Reads icons from a yotoicons.com listing page. The site has no API; each icon is rendered as
/// <c>populate_icon_modal('id', 'category', 'tag 1', 'tag 2', 'artist', 'downloads')</c>, and the page's
/// script sizes its pager with <c>Math.ceil(total / 25)</c>, which is the only place the total appears.
/// </summary>
public static partial class YotoIconsListingParser
{
    private static readonly Uri SiteAddress = new("https://yotoicons.com/");

    /// <summary>Reads a whole listing page: its icons and, when present, the listing's total icon count.</summary>
    public static CommunityCatalogPage ParsePage(string html) => new(Parse(html), TotalCount(html));

    /// <summary>The listing's total icon count from the pager script, or null when the page has none.</summary>
    public static int? TotalCount(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var match = PageCountCall().Match(html);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var total)
            ? total
            : null;
    }

    public static IReadOnlyList<CommunityIcon> Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var icons = new List<CommunityIcon>();
        foreach (Match match in IconModalCall().Matches(html))
        {
            var values = match.Groups.Values.Skip(1).Select(group => Unescape(group.Value)).ToArray();
            if (!IsIconId(values[0]))
            {
                continue;
            }

            icons.Add(ToIcon(values));
        }

        return icons.DistinctBy(icon => icon.Id, StringComparer.Ordinal).ToList();
    }

    private static CommunityIcon ToIcon(string[] values)
    {
        var (id, category, tag1, tag2, artist, downloads) = (values[0], values[1], values[2], values[3], values[4], values[5]);
        var tags = new[] { tag1, tag2, category }.Where(tag => tag.Length > 0).ToList();
        var title = string.Join(" · ", new[] { tag1, tag2 }.Where(tag => tag.Length > 0));
        return new CommunityIcon(
            id,
            title.Length > 0 ? title : category,
            tags,
            artist.Length > 0 ? artist : null,
            int.TryParse(downloads, NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : 0,
            new Uri(SiteAddress, $"static/uploads/{id}.png"));
    }

    private static string Unescape(string value) => WebUtility.HtmlDecode(value.Replace("\\'", "'", StringComparison.Ordinal)).Trim();

    private static bool IsIconId(string value) => value.Length is > 0 and <= 10 && value.All(char.IsAsciiDigit);

    [GeneratedRegex(@"populate_icon_modal\(\s*'((?:[^'\\]|\\.)*)'\s*,\s*'((?:[^'\\]|\\.)*)'\s*,\s*'((?:[^'\\]|\\.)*)'\s*,\s*'((?:[^'\\]|\\.)*)'\s*,\s*'((?:[^'\\]|\\.)*)'\s*,\s*'((?:[^'\\]|\\.)*)'\s*\)", RegexOptions.CultureInvariant)]
    private static partial Regex IconModalCall();

    [GeneratedRegex(@"Math\.ceil\(\s*(\d{1,9})\s*/\s*\d+\s*\)", RegexOptions.CultureInvariant)]
    private static partial Regex PageCountCall();
}
