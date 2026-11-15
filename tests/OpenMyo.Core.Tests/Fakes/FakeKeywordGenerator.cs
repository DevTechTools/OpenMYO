using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Tests.Fakes;

public sealed class FakeKeywordGenerator : IIconKeywordGenerator
{
    public bool IsAvailable { get; init; } = true;

    public IReadOnlyList<IReadOnlyList<string>> Keywords { get; init; } = [];

    public Exception? ThrowOnGenerate { get; init; }

    public int GenerateCalls { get; private set; }

    public IconSuggestionRequest? LastRequest { get; private set; }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) => Task.FromResult(IsAvailable);

    public Task<IReadOnlyList<IReadOnlyList<string>>> GenerateAsync(
        IconSuggestionRequest request, IReadOnlyList<string> vocabulary, CancellationToken cancellationToken)
    {
        GenerateCalls++;
        LastRequest = request;
        return ThrowOnGenerate is null ? Task.FromResult(Keywords) : throw ThrowOnGenerate;
    }
}
