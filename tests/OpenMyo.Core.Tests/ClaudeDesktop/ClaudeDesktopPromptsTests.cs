using OpenMyo.Core.ClaudeDesktop;

namespace OpenMyo.Core.Tests.ClaudeDesktop;

public sealed class ClaudeDesktopPromptsTests
{
    [Fact]
    public void PickIcons_ReplaceExisting_AsksToRepickEveryTrackOfThatPlaylist()
    {
        var prompt = ClaudeDesktopPrompts.PickIcons("Odyssey", "card123", replaceExisting: true);

        Assert.Contains("\"Odyssey\" (id card123)", prompt, StringComparison.Ordinal);
        Assert.Contains("every track", prompt, StringComparison.Ordinal);
        Assert.Contains("replaceExisting set to true", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void PickIcons_MissingOnly_LeavesExistingIconsAlone()
    {
        var prompt = ClaudeDesktopPrompts.PickIcons("Odyssey", "card123", replaceExisting: false);

        Assert.Contains("tracks without one", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("replaceExisting", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void PickIcons_UnsavedPlaylist_NamesItByTitleOnly() =>
        Assert.DoesNotContain("(id", ClaudeDesktopPrompts.PickIcons("Odyssey", null, replaceExisting: true), StringComparison.Ordinal);

    [Fact]
    public void RenameTracks_NamesPlaylistAndRenameTool()
    {
        var prompt = ClaudeDesktopPrompts.RenameTracks("Odyssey", "card123");

        Assert.Contains("\"Odyssey\" (id card123)", prompt, StringComparison.Ordinal);
        Assert.Contains("every track", prompt, StringComparison.Ordinal);
        Assert.Contains("rename_tracks", prompt, StringComparison.Ordinal);
    }
}
