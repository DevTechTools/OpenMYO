namespace OpenMyo.Core.Icons;

/// <summary>A 16x16 icon that a Yoto player can show while a track plays.</summary>
/// <param name="MediaId">Yoto media identifier (the part after <c>yoto:#</c>).</param>
/// <param name="Title">Human readable name; empty for most user-uploaded icons.</param>
/// <param name="Tags">Search tags published by Yoto.</param>
/// <param name="ImageUrl">Where the PNG can be downloaded for previews.</param>
/// <param name="IsPublic">True for Yoto's shared library, false for the user's own uploads.</param>
public sealed record DisplayIcon(
    string MediaId,
    string Title,
    IReadOnlyList<string> Tags,
    Uri? ImageUrl,
    bool IsPublic)
{
    /// <summary>Prefix Yoto uses when a card references media by id.</summary>
    public const string UriPrefix = "yoto:#";

    /// <summary>Converts a media id to the <c>yoto:#id</c> form used in card JSON.</summary>
    public static string ToYotoUri(string mediaId) => UriPrefix + mediaId;

    /// <summary>Extracts the media id from a <c>yoto:#id</c> reference, or null when it is not one.</summary>
    public static string? MediaIdFromUri(string? uri) =>
        uri is not null && uri.StartsWith(UriPrefix, StringComparison.Ordinal) && uri.Length > UriPrefix.Length
            ? uri[UriPrefix.Length..]
            : null;
}
