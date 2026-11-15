using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public class TitleCleanerTests
{
    [Fact]
    public void Clean_MessyFileName_ReturnsReadableTitle()
    {
        var result = TitleCleaner.Clean("03_-_twinkle_twinkle_(Official Audio) [x7Gh2]");

        Assert.Equal("Twinkle Twinkle", result);
    }

    [Fact]
    public void CleanAll_MessyNumberedFileNames_StripsNumbersAndNoise()
    {
        var result = TitleCleaner.CleanAll(["03_-_twinkle_twinkle_(Official Audio) [x7Gh2]", "04_-_baa_baa_black_sheep"]);

        Assert.Equal(["Twinkle Twinkle", "Baa Baa Black Sheep"], result);
    }

    [Fact]
    public void Clean_BareLeadingNumberWithoutFlag_KeepsNumber()
    {
        Assert.Equal("99 Red Balloons", TitleCleaner.Clean("99 Red Balloons"));
    }

    [Fact]
    public void Clean_BareLeadingNumberWithFlag_StripsNumber()
    {
        Assert.Equal("Red Balloons", TitleCleaner.Clean("99 Red Balloons", stripBareNumber: true));
    }

    [Fact]
    public void CleanAll_SingleTitleWithLeadingNumber_KeepsNumber()
    {
        Assert.Equal(["99 Red Balloons"], TitleCleaner.CleanAll(["99 Red Balloons"]));
    }

    [Fact]
    public void CleanAll_EveryTitleNumbered_StripsBareNumbers()
    {
        var result = TitleCleaner.CleanAll(["01 Alpha", "02 Beta"]);

        Assert.Equal(["Alpha", "Beta"], result);
    }

    [Fact]
    public void CleanAll_OnlySomeTitlesNumbered_KeepsUnpaddedBareNumbers()
    {
        var result = TitleCleaner.CleanAll(["1 Alpha", "Beta"]);

        Assert.Equal(["1 Alpha", "Beta"], result);
    }

    [Theory]
    [InlineData("01 Moon Song", "Moon Song")]
    [InlineData("007 Spy Theme", "Spy Theme")]
    [InlineData("99 Red Balloons", "99 Red Balloons")]
    public void Clean_ZeroPaddedNumber_StripsEvenAlone(string raw, string expected)
    {
        Assert.Equal(expected, TitleCleaner.Clean(raw));
    }

    [Theory]
    [InlineData("1-03 Song", "Song")]
    [InlineData("CD2 - 05 Song", "Song")]
    [InlineData("Track 05 - Song", "Song")]
    [InlineData("07. Song", "Song")]
    [InlineData("08) Song", "Song")]
    public void Clean_TrackNumberPrefix_RemovesPrefix(string raw, string expected)
    {
        Assert.Equal(expected, TitleCleaner.Clean(raw));
    }

    [Fact]
    public void CleanAll_SharedArtistPrefix_RemovesPrefix()
    {
        var result = TitleCleaner.CleanAll(["Artist - A", "Artist - B"]);

        Assert.Equal(["A", "B"], result);
    }

    [Fact]
    public void CleanAll_PrefixNotSharedByAll_KeepsTitles()
    {
        var result = TitleCleaner.CleanAll(["Artist - A", "Other - B"]);

        Assert.Equal(["Artist - A", "Other - B"], result);
    }

    [Theory]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("hello world", "Hello World")]
    [InlineData("Hello World", "Hello World")]
    [InlineData("ABC", "ABC")]
    public void Clean_SingleCaseTitle_AppliesTitleCase(string raw, string expected)
    {
        Assert.Equal(expected, TitleCleaner.Clean(raw));
    }

    [Fact]
    public void Clean_DotSeparatedWords_TreatsDotsAsSpaces()
    {
        Assert.Equal("My Song Name", TitleCleaner.Clean("my.song.name"));
    }

    [Fact]
    public void Clean_AbbreviationDotWithSpaces_KeepsDot()
    {
        Assert.Equal("Mr. Brown Goes To Town", TitleCleaner.Clean("Mr. Brown Goes To Town"));
    }

    [Theory]
    [InlineData("Lullaby www.example.com", "Lullaby")]
    [InlineData("Lullaby https://example.org/path", "Lullaby")]
    public void Clean_WebAddress_RemovesAddress(string raw, string expected)
    {
        Assert.Equal(expected, TitleCleaner.Clean(raw));
    }

    [Theory]
    [InlineData("Song (Official Video)", "Song")]
    [InlineData("Song [Lyrics]", "Song")]
    [InlineData("Song (Remastered 2009)", "Song")]
    [InlineData("Song (Live at Home)", "Song (Live at Home)")]
    public void Clean_BracketedNoise_RemovesOnlyNoise(string raw, string expected)
    {
        Assert.Equal(expected, TitleCleaner.Clean(raw));
    }

    [Fact]
    public void Clean_VeryLongTitle_TruncatesToMaxLength()
    {
        var raw = string.Concat(Enumerable.Repeat("Ab", 100));

        var result = TitleCleaner.Clean(raw);

        Assert.Equal(TitleCleaner.MaxTitleLength, result.Length);
    }

    [Fact]
    public void Clean_TitleEmptyAfterCleaning_FallsBackToRaw()
    {
        Assert.Equal("[x7Gh2]", TitleCleaner.Clean("  [x7Gh2]  "));
    }

    [Fact]
    public void Clean_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TitleCleaner.Clean(null!));
    }

    [Fact]
    public void CleanAll_Empty_ReturnsEmpty() => Assert.Empty(TitleCleaner.CleanAll([]));
}
