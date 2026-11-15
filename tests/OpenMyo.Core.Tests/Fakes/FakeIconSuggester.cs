using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Tests.Fakes;

/// <summary>Suggests a fixed icon per track title; titles without an entry get no match.</summary>
public sealed class FakeIconSuggester(IReadOnlyDictionary<string, DisplayIcon> iconsByTitle) : IIconSuggester
{
    public IconSuggestionRequest? LastRequest { get; private set; }

    public Task<IReadOnlyList<IconSuggestion>> SuggestAsync(IconSuggestionRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        IReadOnlyList<IconSuggestion> result = request.TrackTitles
            .Select((title, i) => new IconSuggestion(i, iconsByTitle.TryGetValue(title, out var icon) ? [new IconMatch(icon, 1)] : []))
            .ToList();
        return Task.FromResult(result);
    }
}
