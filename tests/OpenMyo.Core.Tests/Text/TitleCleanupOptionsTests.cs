using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class TitleCleanupOptionsTests
{
    [Fact]
    public void Clean_BracketRuleOff_KeepsEpisodeNumber()
    {
        var options = new TitleCleanupOptions { RemoveBracketedText = false };

        Assert.Equal("[051] Connie, Part 1", TitleCleaner.Clean("[051] Connie, Part 1", options));
    }

    [Fact]
    public void Clean_FindReplace_ReplacesCaseInsensitively()
    {
        var options = new TitleCleanupOptions { Find = "part", Replace = "Pt." };

        Assert.Equal("Connie, Pt. 1", TitleCleaner.Clean("Connie, Part 1", options));
    }

    [Fact]
    public void CleanAll_SharedPrefixRuleOff_KeepsPrefix()
    {
        var options = new TitleCleanupOptions { RemoveSharedPrefix = false };

        var result = TitleCleaner.CleanAll(["Raffi - Shake", "Raffi - Banana"], options);

        Assert.Equal(["Raffi - Shake", "Raffi - Banana"], result);
    }
}
