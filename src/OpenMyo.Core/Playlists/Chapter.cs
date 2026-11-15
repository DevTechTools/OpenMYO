namespace OpenMyo.Core.Playlists;

/// <summary>
/// One row in a playlist as the user sees it. MYO cards normally hold a single track per chapter,
/// and the chapter icon is what the player shows.
/// </summary>
public sealed record Chapter
{
    public required string Key { get; init; }

    public required string Title { get; init; }

    /// <summary>Media id of the 16x16 display icon, or null when none is set.</summary>
    public string? IconMediaId { get; init; }

    public IReadOnlyList<Track> Tracks { get; init; } = [];

    public TimeSpan Duration => Tracks.Aggregate(TimeSpan.Zero, (sum, track) => sum + track.Duration);

    public bool HasIcon => !string.IsNullOrEmpty(IconMediaId);

    /// <summary>
    /// Renames the chapter. A single-track chapter is one row to the user, so its track takes the same title;
    /// tracks in a multi-track chapter keep their own.
    /// </summary>
    public Chapter WithTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        var trimmed = title.Trim();
        var tracks = Tracks.Count == 1 ? [Tracks[0] with { Title = trimmed }] : Tracks;
        return this with { Title = trimmed, Tracks = tracks };
    }
}
