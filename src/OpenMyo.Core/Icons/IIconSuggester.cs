namespace OpenMyo.Core.Icons;

/// <summary>Chooses display icons for tracks.</summary>
public interface IIconSuggester
{
    /// <summary>Returns one suggestion per track title, in the same order.</summary>
    Task<IReadOnlyList<IconSuggestion>> SuggestAsync(IconSuggestionRequest request, CancellationToken cancellationToken);
}
