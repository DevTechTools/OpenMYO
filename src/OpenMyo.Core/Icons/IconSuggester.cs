using Microsoft.Extensions.Logging;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Default suggester. Matches each track title against the icon index with trigram similarity.
/// When the user opts in (<see cref="IconSuggestionRequest.UseAi"/>) and a generator is configured (Claude), it is asked only about tracks the
/// trigram matcher could not match confidently, which keeps AI token use to a minimum.
/// Its keywords are searched first and the plain title remains the fallback.
/// </summary>
public sealed class IconSuggester(
    IconIndexProvider indexProvider,
    ILogger<IconSuggester> logger,
    IIconKeywordGenerator? keywordGenerator = null) : IIconSuggester
{
    private const int CandidatesPerTrack = 12;

    /// <summary>A title match at least this good is trusted without asking the keyword generator.</summary>
    internal const double ConfidentScore = 0.75;

    public async Task<IReadOnlyList<IconSuggestion>> SuggestAsync(IconSuggestionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var index = await indexProvider.GetAsync(cancellationToken);
        var fromTitles = request.TrackTitles.Select(title => index.Search(title, CandidatesPerTrack)).ToList();
        var keywords = await KeywordsForWeakMatchesAsync(request, fromTitles, index, cancellationToken);
        var fallback = index.Search(request.PlaylistTitle, CandidatesPerTrack);

        return fromTitles
            .Select((matches, i) => new IconSuggestion(i, Rank(index, matches, keywords.GetValueOrDefault(i), fallback)))
            .ToList();
    }

    /// <summary>Asks the generator only about tracks without a confident title match. Keys are track indexes.</summary>
    private async Task<Dictionary<int, IReadOnlyList<string>>> KeywordsForWeakMatchesAsync(
        IconSuggestionRequest request, List<IReadOnlyList<IconMatch>> fromTitles, IconIndex index, CancellationToken cancellationToken)
    {
        var weak = Enumerable.Range(0, fromTitles.Count)
            .Where(i => fromTitles[i].Count == 0 || fromTitles[i][0].Score < ConfidentScore)
            .ToList();
        if (weak.Count == 0)
        {
            return [];
        }

        var subset = request with { TrackTitles = weak.Select(i => request.TrackTitles[i]).ToList() };
        var keywords = await TryGenerateKeywordsAsync(subset, index, cancellationToken);
        return keywords is null ? [] : weak.Select((trackIndex, i) => (trackIndex, keywords[i])).ToDictionary();
    }

    private static IReadOnlyList<IconMatch> Rank(
        IconIndex index, IReadOnlyList<IconMatch> fromTitle, IReadOnlyList<string>? aiKeywords, IReadOnlyList<IconMatch> fallback)
    {
        var fromKeywords = aiKeywords is { Count: > 0 } ? SearchEachKeyword(index, aiKeywords) : [];

        var merged = fromKeywords.Concat(fromTitle)
            .DistinctBy(match => match.Icon.MediaId, StringComparer.Ordinal)
            .Take(CandidatesPerTrack)
            .ToList();
        return merged.Count > 0 ? merged : fallback;
    }

    /// <summary>
    /// AI keywords are alternatives ("owl", "moon"), not a conjunction, so each is searched on its own
    /// and results keep the generator's priority order.
    /// </summary>
    private static IEnumerable<IconMatch> SearchEachKeyword(IconIndex index, IReadOnlyList<string> keywords) =>
        keywords.SelectMany(keyword => index.Search(keyword, 3));

    private async Task<IReadOnlyList<IReadOnlyList<string>>?> TryGenerateKeywordsAsync(
        IconSuggestionRequest request, IconIndex index, CancellationToken cancellationToken)
    {
        if (!request.UseAi || keywordGenerator is null || !await keywordGenerator.IsAvailableAsync(cancellationToken))
        {
            return null;
        }

        try
        {
            var keywords = await keywordGenerator.GenerateAsync(request, index.Vocabulary, cancellationToken);
            return keywords.Count == request.TrackTitles.Count ? keywords : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // AI is an enhancement: any failure falls back to plain trigram matching.
            logger.LogWarning(exception, "Keyword generation failed; using trigram matching only");
            return null;
        }
    }
}
