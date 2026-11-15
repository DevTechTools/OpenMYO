namespace OpenMyo.Core.Text;

/// <summary>
/// Title words that rarely say what to picture ("making", "first", "good"). They still count when matching an
/// icon, just much less than the subject words ("rabbit", "grade"). Loaded once from the embedded WeakWords.txt.
/// </summary>
public static class WeakWords
{
    /// <summary>How much a weak word counts compared with a subject word.</summary>
    public const double Weight = 0.25;

    private static readonly HashSet<string> Words =
        EmbeddedLines.Read("OpenMyo.Core.Text.WeakWords.txt").ToHashSet(StringComparer.Ordinal);

    /// <summary>True for a normalized, singular keyword that is filler rather than a subject.</summary>
    public static bool Contains(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        return Words.Contains(word);
    }
}
