using Microsoft.Extensions.Logging.Abstractions;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Icons;

public class IconSuggesterTests
{
    private static readonly DisplayIcon[] Catalog =
    [
        TestIcons.Make("dino", "dinosaur"),
        TestIcons.Make("owl", "owl", "bird"),
        TestIcons.Make("moon", "moon"),
    ];

    private static IconSuggester Create(FakeKeywordGenerator? generator = null) =>
        new(new IconIndexProvider(new FakeIconCatalog(Catalog)), NullLogger<IconSuggester>.Instance, generator);

    // The user has opted in to Claude unless a test says otherwise.
    private static IconSuggestionRequest Request(string playlist, params string[] tracks) => new(playlist, tracks, UseAi: true);

    [Fact]
    public async Task SuggestAsync_NoGenerator_MatchesByTitle()
    {
        var result = await Create().SuggestAsync(Request("Zoo", "Dinosaurs", "Owl"), CancellationToken.None);

        Assert.Equal(["dino", "owl"], result.Select(suggestion => suggestion.Best?.MediaId));
        Assert.Equal([0, 1], result.Select(suggestion => suggestion.TrackIndex));
    }

    [Fact]
    public async Task SuggestAsync_WeakTitleMatch_UsesGeneratorKeywords()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["owl"]] };

        var result = await Create(generator).SuggestAsync(Request("Zoo", "Bedtime Story", "Dinosaurs"), CancellationToken.None);

        Assert.Equal("owl", result[0].Best?.MediaId);
        Assert.Equal("dino", result[1].Best?.MediaId);
    }

    [Fact]
    public async Task SuggestAsync_WeakTitleMatch_SendsOnlyWeakTitlesToGenerator()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["owl"]] };

        await Create(generator).SuggestAsync(Request("Zoo", "Bedtime Story", "Dinosaurs"), CancellationToken.None);

        Assert.Equal(["Bedtime Story"], generator.LastRequest?.TrackTitles);
    }

    [Fact]
    public async Task SuggestAsync_UserDidNotChooseAi_DoesNotCallGenerator()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["owl"]] };

        await Create(generator).SuggestAsync(Request("Zoo", "Bedtime Story") with { UseAi = false }, CancellationToken.None);

        Assert.Equal(0, generator.GenerateCalls);
    }

    [Fact]
    public async Task SuggestAsync_AllTitlesMatchConfidently_DoesNotCallGenerator()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["moon"]] };

        await Create(generator).SuggestAsync(Request("Zoo", "Dinosaurs", "Owl"), CancellationToken.None);

        Assert.Equal(0, generator.GenerateCalls);
    }

    [Fact]
    public async Task SuggestAsync_GeneratorUnavailable_DoesNotCallGenerator()
    {
        var generator = new FakeKeywordGenerator { IsAvailable = false, Keywords = [["owl"]] };

        var result = await Create(generator).SuggestAsync(Request("Zoo", "Dinosaurs"), CancellationToken.None);

        Assert.Equal(0, generator.GenerateCalls);
        Assert.Equal("dino", result[0].Best?.MediaId);
    }

    [Fact]
    public async Task SuggestAsync_GeneratorThrows_FallsBackToTitleMatching()
    {
        var generator = new FakeKeywordGenerator { ThrowOnGenerate = new InvalidOperationException("boom") };

        var result = await Create(generator).SuggestAsync(Request("Zoo", "Dinosaurs"), CancellationToken.None);

        Assert.Equal("dino", result[0].Best?.MediaId);
    }

    [Fact]
    public async Task SuggestAsync_GeneratorCancelled_PropagatesCancellation()
    {
        var generator = new FakeKeywordGenerator { ThrowOnGenerate = new OperationCanceledException() };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Create(generator).SuggestAsync(Request("Zoo", "Bedtime Story"), CancellationToken.None));
    }

    [Fact]
    public async Task SuggestAsync_GeneratorReturnsWrongCount_IsIgnored()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["moon"]] };

        var result = await Create(generator).SuggestAsync(Request("Zoo", "Dinosaurs", "Owl"), CancellationToken.None);

        Assert.Equal(["dino", "owl"], result.Select(suggestion => suggestion.Best?.MediaId));
    }

    [Fact]
    public async Task SuggestAsync_TrackMatchesNothing_FallsBackToPlaylistTitle()
    {
        var result = await Create().SuggestAsync(Request("Dinosaur Party", "Xylophone"), CancellationToken.None);

        Assert.Equal("dino", result[0].Best?.MediaId);
    }

    [Fact]
    public async Task SuggestAsync_NothingMatchesAnywhere_ReturnsEmptySuggestion()
    {
        var result = await Create().SuggestAsync(Request("Xylophone", "Zither"), CancellationToken.None);

        Assert.Null(Assert.Single(result).Best);
    }

    [Fact]
    public async Task SuggestAsync_KeywordAndTitleFindSameIcon_ListsItOnce()
    {
        var generator = new FakeKeywordGenerator { Keywords = [["owl"]] };

        var result = await Create(generator).SuggestAsync(Request("Zoo", "Owl"), CancellationToken.None);

        Assert.Single(result[0].Matches, match => match.Icon.MediaId == "owl");
    }
}
