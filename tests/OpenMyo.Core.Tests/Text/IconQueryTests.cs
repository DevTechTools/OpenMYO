using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class IconQueryTests
{
    [Theory]
    [InlineData("[851] The Sandwich Initiative", "The Sandwich Initiative")]
    [InlineData("[152a] Bonus! Live Q & A with the actors", "Live Q & A with the actors")]
    [InlineData("[859] The Long End, Part 1", "The Long End")]
    [InlineData("01. Twinkle Twinkle (Official Audio)", "Twinkle Twinkle")]
    [InlineData("152a Bonus: Deleted Scene", "Deleted Scene")]
    [InlineData("Track 3 - Owl Song", "Owl Song")]
    [InlineData("[151]", "[151]")]
    public void Clean_RemovesNumbersAndAsides(string title, string expected) =>
        Assert.Equal(expected, IconQuery.Clean(title));

    [Theory]
    [InlineData("[266] It Began with a Rabbit’s Foot", "Rabbit’s Foot")]
    [InlineData("[264] Making the Grade", "Grade")]
    [InlineData("[851] The Sandwich Initiative", "Sandwich")]
    [InlineData("Gone", "Gone")]
    [InlineData("[866] Rumpelstiltskin\uA789 A Wee Little Tale", "Rumpelstiltskin")]
    public void FromTitle_KeepsSubjectWords(string title, string expected) =>
        Assert.Equal(expected, IconQuery.FromTitle(title));

    [Fact]
    public void FromTitle_SeriesWord_IsDropped() =>
        Assert.Equal("Trinity", IconQuery.FromTitle("[858] B-TV: Trinity", new HashSet<string> { "tv" }));
}
