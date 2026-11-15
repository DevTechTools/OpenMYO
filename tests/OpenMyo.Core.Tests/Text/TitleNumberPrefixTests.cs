using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class TitleNumberPrefixTests
{
    [Theory]
    [InlineData("[051] connie_the_cow", "Connie the Cow", "[051] Connie the Cow")]
    [InlineData("Chapter 3 - the wolf", "The Wolf", "Chapter 3: The Wolf")]
    [InlineData("03_the_gruffalo_192kbps", "The Gruffalo", "03 The Gruffalo")]
    [InlineData("12. Bonus", "Bonus", "12 Bonus")]
    public void Restore_NumberDropped_PutsItBack(string original, string rewritten, string expected) =>
        Assert.Equal(expected, TitleNumberPrefix.Restore(original, rewritten));

    [Theory]
    [InlineData("[051] connie", "[051] Connie")]
    [InlineData("chapter 3", "Chapter 3")]
    [InlineData("Chapter 3 the wolf", "Chapter 3: The Wolf")]
    [InlineData("03 song", "03 Song")]
    [InlineData("The Gruffalo", "The Gruffalo")]
    [InlineData("Room on the Broom", "Room On The Broom")]
    public void Restore_NumberKeptOrAbsent_LeavesRewrite(string original, string rewritten) =>
        Assert.Equal(rewritten, TitleNumberPrefix.Restore(original, rewritten));
}
