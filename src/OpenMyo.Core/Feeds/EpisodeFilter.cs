namespace OpenMyo.Core.Feeds;

/// <summary>
/// Narrows a feed preview to the episodes worth downloading: by length and by words in the title.
/// Every condition is optional; an empty filter keeps everything.
/// </summary>
/// <param name="ExcludeWords">An episode whose title contains any of these (ignoring case) is dropped, e.g. "trailer".</param>
/// <param name="IncludeWords">When not empty, only titles containing at least one of these are kept.</param>
public sealed record EpisodeFilter(
    TimeSpan? MinLength,
    TimeSpan? MaxLength,
    IReadOnlyList<string> ExcludeWords,
    IReadOnlyList<string> IncludeWords)
{
    public static EpisodeFilter None { get; } = new(null, null, [], []);

    /// <summary>
    /// Episodes whose feed gives no length pass the length limits: there is nothing to judge them by
    /// until they are downloaded.
    /// </summary>
    public bool Matches(FeedEpisode episode)
    {
        ArgumentNullException.ThrowIfNull(episode);
        return IsLengthAllowed(episode.Duration)
            && !ExcludeWords.Any(word => Contains(episode.Title, word))
            && (IncludeWords.Count == 0 || IncludeWords.Any(word => Contains(episode.Title, word)));
    }

    /// <summary>Splits "trailer, bonus ,rerun" into its words; blank entries are dropped.</summary>
    public static IReadOnlyList<string> ParseWords(string? text) =>
        (text ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private bool IsLengthAllowed(TimeSpan? duration) =>
        duration is not { } length || ((MinLength is null || length >= MinLength) && (MaxLength is null || length <= MaxLength));

    private static bool Contains(string title, string word) => title.Contains(word, StringComparison.CurrentCultureIgnoreCase);
}
