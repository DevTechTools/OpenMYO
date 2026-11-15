using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Library;

/// <summary>Evaluates a playlist for the problems the library can filter on.</summary>
public static class PlaylistHealth
{
    public static PlaylistIssues Evaluate(Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        var issues = PlaylistIssues.None;
        if (!playlist.Chapters.Any(chapter => chapter.Tracks.Count > 0))
        {
            issues |= PlaylistIssues.NoTracks;
        }

        if (playlist.Chapters.Any(chapter => !chapter.HasIcon))
        {
            issues |= PlaylistIssues.MissingIcons;
        }

        if (playlist.CoverImageUrl is null)
        {
            issues |= PlaylistIssues.MissingCover;
        }

        return issues;
    }
}
