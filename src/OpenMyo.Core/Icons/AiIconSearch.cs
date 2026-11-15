using System.Collections.Concurrent;

namespace OpenMyo.Core.Icons;

/// <summary>
/// "Search with AI" for the icon picker: asks the keyword generator (Claude) for drawable keywords for one
/// title, then matches them locally. Runs only when the user asks, and caches answers per title so
/// repeating a search costs no tokens.
/// </summary>
public sealed class AiIconSearch(IconIndexProvider indexProvider, IIconKeywordGenerator? keywordGenerator = null)
{
    private const int ResultsPerKeyword = 12;

    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _keywordCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        keywordGenerator is not null && await keywordGenerator.IsAvailableAsync(cancellationToken);

    /// <returns>The keywords Claude chose (shown to the user) and the icons they matched, best first.</returns>
    public async Task<AiSearchResult> SearchAsync(string title, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (keywordGenerator is null)
        {
            return new AiSearchResult([], []);
        }

        var index = await indexProvider.GetAsync(cancellationToken);
        if (!_keywordCache.TryGetValue(title, out var keywords))
        {
            var request = new IconSuggestionRequest(string.Empty, [title], UseAi: true);
            var generated = await keywordGenerator.GenerateAsync(request, index.Vocabulary, cancellationToken);
            keywords = generated.Count == 1 ? generated[0] : [];
            _keywordCache[title] = keywords;
        }

        var matches = keywords
            .SelectMany(keyword => index.Search(keyword, ResultsPerKeyword))
            .DistinctBy(match => match.Icon.MediaId, StringComparer.Ordinal)
            .ToList();
        return new AiSearchResult(keywords, matches);
    }
}

/// <summary>Keywords chosen by the AI and the icons they matched.</summary>
public sealed record AiSearchResult(IReadOnlyList<string> Keywords, IReadOnlyList<IconMatch> Matches);
