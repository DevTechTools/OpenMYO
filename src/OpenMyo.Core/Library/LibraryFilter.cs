namespace OpenMyo.Core.Library;

/// <summary>
/// The library's filter chips. A filter is a set of <see cref="PlaylistIssues"/>: chips combine, so a playlist
/// shows only when it has every issue chosen. <see cref="PlaylistIssues.None"/> shows everything.
/// </summary>
public static class LibraryFilter
{
    /// <summary>True when a playlist with <paramref name="issues"/> has every issue in <paramref name="required"/>.</summary>
    public static bool Matches(PlaylistIssues required, PlaylistIssues issues) => (issues & required) == required;
}
