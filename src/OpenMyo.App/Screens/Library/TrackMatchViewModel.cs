using OpenMyo.Core.Library;

namespace OpenMyo.App.Screens.Library;

/// <summary>A matching track shown under its playlist in the library; clicking it opens the playlist at that track.</summary>
public sealed class TrackMatchViewModel(PlaylistRowViewModel row, TrackMatch match)
{
    public PlaylistRowViewModel Row { get; } = row;

    public string ChapterKey => match.ChapterKey;

    /// <summary>"♪ 3 ·": the track's position, as the player and the editor number it.</summary>
    public string NumberLabel => $"♪ {match.Number} ·";

    public IReadOnlyList<TextSegment> Segments { get; } = TextSegment.Split(match.Title, match.Ranges);

    public string AccessibleName => $"Track {match.Number}, {match.Title}";
}
