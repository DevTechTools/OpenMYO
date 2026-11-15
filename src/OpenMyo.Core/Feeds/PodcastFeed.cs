namespace OpenMyo.Core.Feeds;

/// <summary>A podcast or audio RSS feed, read but not downloaded.</summary>
/// <param name="Url">The address the feed was read from.</param>
/// <param name="ImageUrl">The show's artwork, used as the playlist cover.</param>
/// <param name="Episodes">Episodes with audio, in listening order (oldest first).</param>
public sealed record PodcastFeed(Uri Url, string Title, Uri? ImageUrl, IReadOnlyList<FeedEpisode> Episodes);
