namespace OpenMyo.Core.Icons;

/// <summary>
/// One search keyword, its trigrams, related words that also count (exactly, at a discount), and how much the
/// keyword counts towards the score (filler words count less than subject words).
/// </summary>
internal sealed record QueryWord(string Word, long[] Trigrams, IReadOnlyList<string> Synonyms, double Weight);
