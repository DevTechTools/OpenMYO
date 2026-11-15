namespace OpenMyo.Core.Icons;

/// <summary>
/// Optional AI step: turns track titles into concrete, drawable keywords
/// ("The Owl and the Pussycat" → owl, cat, boat, moon) that the trigram index can match.
/// </summary>
public interface IIconKeywordGenerator
{
    /// <summary>True when the user enabled AI in Settings and the generator is configured (e.g. an API key is stored).</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>Returns keywords per track, in the same order as the request's titles.</summary>
    /// <param name="vocabulary">Words the icon index knows; generators should prefer these.</param>
    Task<IReadOnlyList<IReadOnlyList<string>>> GenerateAsync(
        IconSuggestionRequest request,
        IReadOnlyList<string> vocabulary,
        CancellationToken cancellationToken);
}
