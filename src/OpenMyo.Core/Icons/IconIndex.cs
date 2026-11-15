using OpenMyo.Core.Text;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Immutable, precomputed search index over display icons. Build once per catalog load and query
/// many times. Lookup uses an inverted trigram index to find candidates, then scores only those.
/// </summary>
public sealed class IconIndex
{
    private const double TitleWeight = 1.0;
    private const double TagWeight = 0.9;

    /// <summary>Caps the vocabulary so prompts that include it stay small (token cost).</summary>
    private const int MaxVocabularyWords = 1500;

    private readonly DisplayIcon[] _icons;
    private readonly IconTerm[][] _terms;
    private readonly Dictionary<long, int[]> _postings;
    private readonly Dictionary<string, DisplayIcon> _byMediaId;
    private readonly IReadOnlyDictionary<int, DisplayIcon> _numbers;

    private IconIndex(DisplayIcon[] icons, IconTerm[][] terms, Dictionary<long, int[]> postings)
    {
        _icons = icons;
        _terms = terms;
        _postings = postings;
        _byMediaId = icons.ToDictionary(icon => icon.MediaId, StringComparer.Ordinal);
        _numbers = NumberIcons.From(icons);
        Vocabulary = BuildVocabulary(terms);
    }

    public static IconIndex Empty { get; } = Build([]);

    /// <summary>
    /// The most widely used searchable words (up to 1,500), sorted alphabetically so the list is stable
    /// between calls. Used as the vocabulary for keyword generation.
    /// </summary>
    public IReadOnlyList<string> Vocabulary { get; }

    public int Count => _icons.Length;

    public static IconIndex Build(IEnumerable<DisplayIcon> icons)
    {
        ArgumentNullException.ThrowIfNull(icons);

        var distinct = icons.DistinctBy(icon => icon.MediaId, StringComparer.Ordinal).ToArray();
        var terms = distinct.Select(BuildTerms).ToArray();
        return new IconIndex(distinct, terms, BuildPostings(terms));
    }

    public DisplayIcon? Find(string? mediaId) =>
        mediaId is not null && _byMediaId.TryGetValue(mediaId, out var icon) ? icon : null;

    /// <summary>Yoto's icon showing <paramref name="number"/>, or null when Yoto has none for it.</summary>
    public DisplayIcon? NumberIcon(int number) => _numbers.GetValueOrDefault(number);

    /// <summary>Searches with free text, e.g. a track title.</summary>
    public IReadOnlyList<IconMatch> Search(string text, int maxResults) =>
        Search([text], maxResults);

    /// <summary>Searches with already-extracted keywords. Every keyword contributes equally.</summary>
    public IReadOnlyList<IconMatch> Search(IReadOnlyList<string> keywords, int maxResults)
    {
        ArgumentNullException.ThrowIfNull(keywords);

        // Keywords may be phrases ("night sky"); split them into the same word form the index uses.
        var queries = keywords.SelectMany(TextNormalizer.Keywords)
            .Distinct(StringComparer.Ordinal)
            .Select(word => new QueryWord(word, Trigrams.Of(word), WordSynonyms.For(word), WeakWords.Contains(word) ? WeakWords.Weight : 1))
            .ToList();
        if (queries.Count == 0)
        {
            return [];
        }

        var scorer = new IconScorer(queries);
        var trigrams = queries.SelectMany(q => q.Synonyms.Select(Trigrams.Of).Prepend(q.Trigrams)).SelectMany(t => t);
        return FindCandidates(trigrams)
            .Select(index => new IconMatch(_icons[index], scorer.Score(_terms[index])))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Icon.Title.Length)
            .Take(maxResults)
            .ToList();
    }

    private HashSet<int> FindCandidates(IEnumerable<long> trigrams)
    {
        var candidates = new HashSet<int>();
        foreach (var trigram in trigrams)
        {
            if (_postings.TryGetValue(trigram, out var iconIndexes))
            {
                candidates.UnionWith(iconIndexes);
            }
        }

        return candidates;
    }

    private static IconTerm[] BuildTerms(DisplayIcon icon)
    {
        var titleWords = TextNormalizer.Keywords(icon.Title).Select(word => (Word: word, Weight: TitleWeight));
        var tagWords = icon.Tags.SelectMany(TextNormalizer.Keywords).Select(word => (Word: word, Weight: TagWeight));

        // Title words come first, so DistinctBy keeps the higher weight when a tag repeats the title.
        return titleWords.Concat(tagWords)
            .DistinctBy(pair => pair.Word, StringComparer.Ordinal)
            .Select(pair => new IconTerm(pair.Word, Trigrams.Of(pair.Word), pair.Weight))
            .ToArray();
    }

    private static List<string> BuildVocabulary(IconTerm[][] terms) =>
        terms.SelectMany(t => t).GroupBy(term => term.Word, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(MaxVocabularyWords)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static Dictionary<long, int[]> BuildPostings(IconTerm[][] terms)
    {
        var postings = new Dictionary<long, List<int>>();
        for (var iconIndex = 0; iconIndex < terms.Length; iconIndex++)
        {
            foreach (var trigram in terms[iconIndex].SelectMany(term => term.Trigrams).Distinct())
            {
                if (!postings.TryGetValue(trigram, out var list))
                {
                    postings[trigram] = list = [];
                }

                list.Add(iconIndex);
            }
        }

        return postings.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }
}
