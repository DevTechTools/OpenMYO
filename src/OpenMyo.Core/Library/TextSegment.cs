namespace OpenMyo.Core.Library;

/// <summary>A run of text that is either a search match (highlighted) or plain.</summary>
public sealed record TextSegment(string Text, bool IsMatch)
{
    /// <summary>
    /// Cuts <paramref name="text"/> at the edges of <paramref name="ranges"/>, which must be sorted and not
    /// overlap (as <see cref="SearchQuery.Match"/> returns them). No ranges gives one plain segment.
    /// </summary>
    public static IReadOnlyList<TextSegment> Split(string text, IReadOnlyList<TextMatch>? ranges)
    {
        ArgumentNullException.ThrowIfNull(text);

        var segments = new List<TextSegment>();
        var position = 0;
        foreach (var range in ranges ?? [])
        {
            AddPlain(segments, text, position, range.Start);
            segments.Add(new TextSegment(text.Substring(range.Start, range.Length), IsMatch: true));
            position = range.Start + range.Length;
        }

        AddPlain(segments, text, position, text.Length);
        return segments;
    }

    private static void AddPlain(List<TextSegment> segments, string text, int start, int end)
    {
        if (end > start)
        {
            segments.Add(new TextSegment(text[start..end], IsMatch: false));
        }
    }
}
