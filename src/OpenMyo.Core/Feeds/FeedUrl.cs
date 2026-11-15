namespace OpenMyo.Core.Feeds;

/// <summary>Turns what the user typed or pasted into a feed address that can be fetched.</summary>
public static class FeedUrl
{
    // Podcast apps hand out subscribe links with these schemes; they all mean "this feed over HTTPS".
    private static readonly string[] PodcastSchemes = ["feed", "pcast", "itpc", "podcast"];

    /// <summary>
    /// Accepts http(s) links, podcast subscribe links (<c>feed://</c>, <c>itpc://</c> …) and bare addresses
    /// like <c>example.com/feed.xml</c>. Returns null when the text is not a web address.
    /// </summary>
    public static Uri? TryParse(string? text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Any(char.IsWhiteSpace))
        {
            return null;
        }

        var withScheme = trimmed.Contains("://", StringComparison.Ordinal) ? ReplacePodcastScheme(trimmed) : "https://" + trimmed;
        return Uri.TryCreate(withScheme, UriKind.Absolute, out var uri) && IsWeb(uri) && uri.Host.Contains('.', StringComparison.Ordinal)
            ? uri
            : null;
    }

    /// <summary>True for absolute http and https addresses, the only ones OpenMyo will fetch.</summary>
    public static bool IsWeb(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private static string ReplacePodcastScheme(string text)
    {
        var separator = text.IndexOf("://", StringComparison.Ordinal);
        var scheme = text[..separator];
        var rest = text[(separator + 3)..];
        if (!PodcastSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
        {
            return text;
        }

        // "feed://https://example.com/rss" wraps a full address; "feed://example.com/rss" does not.
        return rest.Contains("://", StringComparison.Ordinal) ? rest : "https://" + rest;
    }
}
