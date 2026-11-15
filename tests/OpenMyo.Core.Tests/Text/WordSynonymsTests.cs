using OpenMyo.Core.Icons;
using OpenMyo.Core.Tests.Fakes;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class WordSynonymsTests
{
    [Fact]
    public void Parse_SkipsCommentsAndTrimsWords()
    {
        var map = WordSynonyms.Parse(["# comment", "", "trivia: question , quiz"]);

        Assert.Equal(["question", "quiz"], Assert.Single(map).Value);
    }

    [Fact]
    public void For_ShippedList_MapsTriviaToQuestion() =>
        Assert.Contains("question", WordSynonyms.For("trivia"));

    [Fact]
    public void Search_TitleWordWithSynonym_FindsIconTaggedWithIt()
    {
        var index = IconIndex.Build([TestIcons.Make("q", "Question mark"), TestIcons.Make("t", "Tree")]);

        var match = Assert.Single(index.Search("Trivia", 5));

        Assert.Equal("q", match.Icon.MediaId);
    }

    [Fact]
    public void Search_DirectWordAndSynonym_DirectWordRanksFirst()
    {
        var index = IconIndex.Build([TestIcons.Make("q", "Question"), TestIcons.Make("t", "Trivia night")]);

        Assert.Equal("t", index.Search("Trivia", 5)[0].Icon.MediaId);
    }

    [Fact]
    public void Search_WeakAndSubjectWord_SubjectWordDecides()
    {
        var index = IconIndex.Build([TestIcons.Make("good", "Good"), TestIcons.Make("grade", "Grade")]);

        Assert.Equal("grade", index.Search("Good grade", 5)[0].Icon.MediaId);
    }
}
