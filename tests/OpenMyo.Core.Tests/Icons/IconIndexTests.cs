using OpenMyo.Core.Icons;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Icons;

public class IconIndexTests
{
    [Fact]
    public void Search_ExactTitleAndTagMatch_TitleRanksFirst()
    {
        var index = IconIndex.Build([TestIcons.Make("tagged", "pet", "cat"), TestIcons.Make("titled", "cat")]);

        var matches = index.Search("cat", 5);

        Assert.Equal(["titled", "tagged"], matches.Select(match => match.Icon.MediaId));
        Assert.Equal(1.0, matches[0].Score);
    }

    [Theory]
    [InlineData("dinosaurs")]
    [InlineData("dinosaurus")]
    public void Search_NearMiss_FindsIcon(string query)
    {
        var index = IconIndex.Build([TestIcons.Make("dino", "dinosaur"), TestIcons.Make("moon", "moon")]);

        var matches = index.Search(query, 5);

        Assert.Equal("dino", Assert.Single(matches).Icon.MediaId);
    }

    [Fact]
    public void Search_MultiWordQuery_RanksIconMatchingMoreWordsFirst()
    {
        var index = IconIndex.Build([TestIcons.Make("moon", "moon"), TestIcons.Make("both", "moon star")]);

        var matches = index.Search("moon star", 5);

        Assert.Equal(["both", "moon"], matches.Select(match => match.Icon.MediaId));
        Assert.True(matches[0].Score > matches[1].Score);
    }

    [Fact]
    public void Search_EqualScores_ShorterTitleFirst()
    {
        var index = IconIndex.Build([TestIcons.Make("long", "cat", "x"), TestIcons.Make("longer", "cat")]);

        var matches = index.Search("cat", 5);

        Assert.Equal(2, matches.Count);
        Assert.True(matches[0].Icon.Title.Length <= matches[1].Icon.Title.Length);
    }

    [Fact]
    public void Search_MaxResults_LimitsResults()
    {
        var index = IconIndex.Build([TestIcons.Make("a", "cat"), TestIcons.Make("b", "cat"), TestIcons.Make("c", "cat")]);

        Assert.Equal(2, index.Search("cat", 2).Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("the of and")]
    public void Search_EmptyOrStopWordQuery_ReturnsNothing(string query)
    {
        var index = IconIndex.Build([TestIcons.Make("cat", "cat")]);

        Assert.Empty(index.Search(query, 5));
    }

    [Fact]
    public void Search_UnrelatedQuery_ReturnsNothing()
    {
        var index = IconIndex.Build([TestIcons.Make("cat", "cat")]);

        Assert.Empty(index.Search("xylophone", 5));
    }

    [Fact]
    public void Search_KeywordList_PhrasesAreSplitIntoWords()
    {
        var index = IconIndex.Build([TestIcons.Make("sky", "night sky")]);

        var matches = index.Search(["night sky"], 5);

        Assert.Equal(1.0, Assert.Single(matches).Score);
    }

    [Fact]
    public void Build_DuplicateMediaIds_KeepsFirstOnly()
    {
        var index = IconIndex.Build([TestIcons.Make("same", "first"), TestIcons.Make("same", "second")]);

        Assert.Equal(1, index.Count);
        Assert.Equal("first", index.Find("same")?.Title);
    }

    [Fact]
    public void Find_UnknownOrNullId_ReturnsNull()
    {
        var index = IconIndex.Build([TestIcons.Make("a", "cat")]);

        Assert.Null(index.Find("missing"));
        Assert.Null(index.Find(null));
    }

    [Fact]
    public void Vocabulary_TitlesAndTags_AreSortedAndDistinct()
    {
        var index = IconIndex.Build([TestIcons.Make("1", "Zebra Star", "ant"), TestIcons.Make("2", "star", "ant", "Stars")]);

        Assert.Equal(["ant", "star", "zebra"], index.Vocabulary);
    }

    [Fact]
    public void Empty_HasNoIconsOrVocabulary()
    {
        Assert.Equal(0, IconIndex.Empty.Count);
        Assert.Empty(IconIndex.Empty.Vocabulary);
        Assert.Empty(IconIndex.Empty.Search("cat", 5));
    }
}
