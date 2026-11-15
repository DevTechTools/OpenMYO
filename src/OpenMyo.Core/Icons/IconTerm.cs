namespace OpenMyo.Core.Icons;

/// <summary>A searchable word attached to an icon (from its title or tags) with precomputed trigrams.</summary>
/// <param name="Weight">Title words count fully; tags slightly less because they are broader.</param>
internal readonly record struct IconTerm(string Word, long[] Trigrams, double Weight);
