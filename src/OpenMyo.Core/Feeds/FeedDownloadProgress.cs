namespace OpenMyo.Core.Feeds;

/// <summary>Progress of a feed download, overall and for the episode that just moved.</summary>
/// <param name="Fraction">All episodes together, 0 to 1.</param>
/// <param name="Episode">The episode this update is about; null for overall-only updates such as the artwork.</param>
/// <param name="EpisodeFraction">How far <paramref name="Episode"/> has got, 0 to 1.</param>
public sealed record FeedDownloadProgress(double Fraction, string Status, FeedEpisode? Episode = null, double EpisodeFraction = 0);
