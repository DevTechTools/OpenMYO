using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public class TextNormalizerTests
{
    [Fact]
    public void Normalize_AccentedText_StripsAccentsAndLowerCases()
    {
        Assert.Equal("cafe nandu", TextNormalizer.Normalize("Café Ñandú"));
    }

    [Fact]
    public void Normalize_Punctuation_BecomesSpaces()
    {
        Assert.Equal("a b c", TextNormalizer.Normalize("a-b,c"));
    }

    [Fact]
    public void Keywords_StopWordsAndSingleCharacters_AreDropped()
    {
        var result = TextNormalizer.Keywords("The Owl and the Pussycat a");

        Assert.Equal(["owl", "pussycat"], result);
    }

    [Fact]
    public void Keywords_NumbersAndDuplicates_AreDropped()
    {
        var result = TextNormalizer.Keywords("Star 123 stars star");

        Assert.Equal(["star"], result);
    }

    [Fact]
    public void Keywords_OnlyStopWords_ReturnsEmpty()
    {
        Assert.Empty(TextNormalizer.Keywords("The Song of the Chapter"));
    }

    [Theory]
    [InlineData("stars", "star")]
    [InlineData("foxes", "fox")]
    [InlineData("churches", "church")]
    [InlineData("berries", "berry")]
    [InlineData("bus", "bus")]
    [InlineData("glass", "glass")]
    [InlineData("cat", "cat")]
    [InlineData("pies", "pie")]
    public void Singularize_CommonPlurals_ReturnsStem(string word, string expected)
    {
        Assert.Equal(expected, TextNormalizer.Singularize(word));
    }
}
