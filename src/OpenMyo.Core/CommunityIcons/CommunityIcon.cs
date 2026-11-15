namespace OpenMyo.Core.CommunityIcons;

/// <summary>An icon shared by another Yoto parent on a community site (yotoicons.com). Not yet in the user's account.</summary>
/// <param name="Id">The site's icon id.</param>
/// <param name="Title">The site's title, built from its tags.</param>
/// <param name="Artist">Uploader handle, shown as credit; null when unknown.</param>
/// <param name="Downloads">Popularity on the site; used to break ranking ties.</param>
public sealed record CommunityIcon(
    string Id,
    string Title,
    IReadOnlyList<string> Tags,
    string? Artist,
    int Downloads,
    Uri ImageUrl)
{
    public string Credit => Artist is null ? "yotoicons.com" : $"by {Artist} · yotoicons.com";
}
