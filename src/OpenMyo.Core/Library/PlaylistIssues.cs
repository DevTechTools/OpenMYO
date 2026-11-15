namespace OpenMyo.Core.Library;

/// <summary>Problems a playlist can have. Combined as flags so one scan answers every filter.</summary>
[Flags]
public enum PlaylistIssues
{
    None = 0,

    /// <summary>The card has no playable tracks.</summary>
    NoTracks = 1 << 0,

    /// <summary>At least one chapter has no display icon.</summary>
    MissingIcons = 1 << 1,

    /// <summary>The card has no cover image.</summary>
    MissingCover = 1 << 2,
}
