using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public class TrigramsTests
{
    [Fact]
    public void Similarity_IdenticalWords_ReturnsOne()
    {
        Assert.Equal(1.0, Trigrams.Similarity(Trigrams.Of("dinosaur"), Trigrams.Of("dinosaur")));
    }

    [Fact]
    public void Similarity_DisjointWords_ReturnsZero()
    {
        Assert.Equal(0.0, Trigrams.Similarity(Trigrams.Of("abc"), Trigrams.Of("xyz")));
    }

    [Fact]
    public void Similarity_ReversedArguments_IsSymmetric()
    {
        var forward = Trigrams.Similarity(Trigrams.Of("star"), Trigrams.Of("stars"));
        var backward = Trigrams.Similarity(Trigrams.Of("stars"), Trigrams.Of("star"));

        Assert.Equal(forward, backward);
    }

    [Fact]
    public void Similarity_SingularAndPlural_ScoresFarAboveUnrelatedWords()
    {
        var related = Trigrams.Similarity(Trigrams.Of("star"), Trigrams.Of("stars"));
        var unrelated = Trigrams.Similarity(Trigrams.Of("star"), Trigrams.Of("moon"));

        Assert.InRange(related, 0.3, 0.99);
        Assert.True(related > unrelated);
    }

    [Fact]
    public void Similarity_EmptySet_ReturnsZero()
    {
        Assert.Equal(0.0, Trigrams.Similarity([], Trigrams.Of("star")));
    }

    [Fact]
    public void Of_RepeatedLetters_ReturnsSortedDistinctTrigrams()
    {
        var result = Trigrams.Of("aaaa");

        Assert.Equal(result.Distinct().Order().ToArray(), result);
    }

    [Fact]
    public void Of_Word_PadsWithSpaces()
    {
        // "  c", " ca", "cat", "at " for "cat".
        Assert.Equal(4, Trigrams.Of("cat").Length);
    }
}
