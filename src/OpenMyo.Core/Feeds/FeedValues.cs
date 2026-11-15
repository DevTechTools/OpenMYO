using System.Globalization;

namespace OpenMyo.Core.Feeds;

/// <summary>Parses the loosely formatted dates and durations found in real-world RSS feeds.</summary>
public static class FeedValues
{
    // RFC 822 allows named US zones; DateTimeOffset only understands numeric offsets.
    private static readonly Dictionary<string, string> NamedZones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GMT"] = "+00:00",
        ["UT"] = "+00:00",
        ["UTC"] = "+00:00",
        ["Z"] = "+00:00",
        ["EST"] = "-05:00",
        ["EDT"] = "-04:00",
        ["CST"] = "-06:00",
        ["CDT"] = "-05:00",
        ["MST"] = "-07:00",
        ["MDT"] = "-06:00",
        ["PST"] = "-08:00",
        ["PDT"] = "-07:00",
    };

    /// <summary>Reads an RSS <c>pubDate</c> such as <c>Tue, 03 Jun 2025 09:00:00 +0000</c>; null when unreadable.</summary>
    public static DateTimeOffset? ParseDate(string? text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        var normalised = NormaliseZone(trimmed);
        return DateTimeOffset.TryParse(normalised, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var date)
            ? date
            : null;
    }

    /// <summary>Reads an <c>itunes:duration</c>: <c>H:MM:SS</c>, <c>MM:SS</c> or plain seconds; null when unreadable.</summary>
    public static TimeSpan? ParseDuration(string? text)
    {
        var parts = text?.Trim().Split(':');
        if (parts is null || parts.Length > 3)
        {
            return null;
        }

        var seconds = 0.0;
        foreach (var part in parts)
        {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0)
            {
                return null;
            }

            seconds = (seconds * 60) + value;
        }

        return seconds > 0 ? TimeSpan.FromSeconds(seconds) : null;
    }

    private static string NormaliseZone(string text)
    {
        var lastSpace = text.LastIndexOf(' ');
        var zone = lastSpace < 0 ? string.Empty : text[(lastSpace + 1)..];
        if (NamedZones.TryGetValue(zone, out var offset))
        {
            return text[..lastSpace] + " " + offset;
        }

        // "+0000" → "+00:00"
        return zone is [('+' or '-'), _, _, _, _] && zone[1..].All(char.IsAsciiDigit)
            ? string.Concat(text.AsSpan(0, lastSpace + 1), zone.AsSpan(0, 3), ":", zone.AsSpan(3))
            : text;
    }
}
