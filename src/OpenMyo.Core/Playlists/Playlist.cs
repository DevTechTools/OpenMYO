namespace OpenMyo.Core.Playlists;

/// <summary>A Make Your Own (MYO) card: an ordered list of chapters with optional cover art.</summary>
public sealed record Playlist
{
    /// <summary>Yoto card id; null until the playlist has been saved for the first time.</summary>
    public string? CardId { get; init; }

    public required string Title { get; init; }

    public Uri? CoverImageUrl { get; init; }

    public IReadOnlyList<Chapter> Chapters { get; init; } = [];

    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Total playing time across every track.</summary>
    public TimeSpan Duration => Chapters.Aggregate(TimeSpan.Zero, (sum, chapter) => sum + chapter.Duration);

    /// <summary>Total size in bytes across every track.</summary>
    public long FileSize => Chapters.Sum(chapter => chapter.Tracks.Sum(track => track.FileSize));
}
