using System.Text.RegularExpressions;

namespace OpenMyo.Yoto.Icons;

/// <summary>
/// Where Yoto serves display icon images. Cards often use icons that are not in the public catalog
/// (the user's own uploads or icons shared by other creators); every icon is served from this address.
/// </summary>
public static partial class YotoIconUrls
{
    private const string IconBaseUrl = "https://media-secure-v2.api.yotoplay.com/icons/";

    /// <summary>Returns the image URL for a media id, or null when the id is not a valid Yoto media id.</summary>
    public static Uri? For(string? mediaId) =>
        mediaId is not null && MediaId().IsMatch(mediaId) ? new Uri(IconBaseUrl + mediaId) : null;

    [GeneratedRegex("^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex MediaId();
}
