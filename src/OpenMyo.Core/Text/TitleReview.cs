namespace OpenMyo.Core.Text;

/// <summary>
/// What could be tidied in a playlist's titles, so the editor can suggest a fix only when one would help.
/// </summary>
/// <param name="TidyCount">How many titles <see cref="TitleCleaner"/> would change.</param>
/// <param name="TidyExample">One before → after pair to show what "clean up" will do, or null.</param>
/// <param name="IsOutOfOrder">True when the titles are numbered but not in number order (see <see cref="TitleNumbers"/>).</param>
public sealed record TitleReview(int TidyCount, string? TidyExample, bool IsOutOfOrder)
{
    public bool HasSuggestions => TidyCount > 0 || IsOutOfOrder;

    public static TitleReview Evaluate(IReadOnlyList<string> titles)
    {
        ArgumentNullException.ThrowIfNull(titles);

        var cleaned = TitleCleaner.CleanAll(titles, TitleCleanupOptions.SuitedTo(titles));
        // Stray spaces are trimmed on save anyway; only visible changes are worth suggesting.
        var changed = Enumerable.Range(0, titles.Count).Where(i => !string.Equals(titles[i].Trim(), cleaned[i], StringComparison.Ordinal)).ToList();
        var example = changed.Count == 0 ? null : $"{titles[changed[0]]} → {cleaned[changed[0]]}";
        return new TitleReview(changed.Count, example, TitleNumbers.IsOutOfOrder(titles));
    }
}
