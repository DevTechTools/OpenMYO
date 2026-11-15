using OpenMyo.Core.Text;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Scores an icon against query keywords. Each keyword takes its best similarity among the icon's
/// terms; weak similarities are ignored so "star" does not match "start" too eagerly.
/// The final score is the weighted average over keywords, so icons matching more of the query (and above all
/// its subject words) rank higher.
/// A keyword's synonym counts only as an exact word, and for less than the keyword itself.
/// </summary>
internal sealed class IconScorer(IReadOnlyList<QueryWord> queries)
{
    internal const double SynonymWeight = 0.8;

    /// <summary>Below this trigram similarity a keyword is considered unmatched.</summary>
    internal const double MinimumWordSimilarity = 0.4;

    public double Score(IconTerm[] terms)
    {
        if (terms.Length == 0)
        {
            return 0;
        }

        var total = 0.0;
        var weights = 0.0;
        foreach (var query in queries)
        {
            total += query.Weight * BestSimilarity(query, terms);
            weights += query.Weight;
        }

        return total / weights;
    }

    private static double BestSimilarity(QueryWord query, IconTerm[] terms)
    {
        var best = 0.0;
        foreach (var term in terms)
        {
            var similarity = string.Equals(term.Word, query.Word, StringComparison.Ordinal) ? 1.0
                : query.Synonyms.Contains(term.Word) ? SynonymWeight
                : Trigrams.Similarity(query.Trigrams, term.Trigrams);
            best = Math.Max(best, similarity * term.Weight);
        }

        return best >= MinimumWordSimilarity ? best : 0;
    }
}
