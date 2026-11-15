namespace OpenMyo.Core.Feeds;

/// <summary>One feed item that has an audio enclosure.</summary>
/// <param name="ContentType">The enclosure's declared MIME type, used to pick a file extension; may be wrong or missing.</param>
public sealed record FeedEpisode(
    string Title,
    Uri AudioUrl,
    DateTimeOffset? Published,
    TimeSpan? Duration,
    string? ContentType);
