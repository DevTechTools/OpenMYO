using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol;
using OpenMyo.Core.Text;
using OpenMyo.Mcp.Playlists;

namespace OpenMyo.Mcp.Tests.Playlists;

public sealed class PlaylistEditToolsTests
{
    [Fact]
    public async Task RenamePlaylist_ValidTitle_SavesTrimmedTitleAndKeepsTracks()
    {
        var world = new McpWorld();

        var result = await world.PlaylistEdit.RenamePlaylist(McpWorld.CardId, "  Bedtime Stories  ");

        var saved = Assert.Single(world.Repository.Saved);
        Assert.Equal("Bedtime Stories", saved.Title);
        Assert.Equal("Chapter 1 - The Rabbit", saved.Chapters[0].Title);
        Assert.Equal(new RenamePlaylistResult("Bedtime Stories", "Bedtime"), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenamePlaylist_BlankTitle_ThrowsAndSavesNothing(string title)
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.PlaylistEdit.RenamePlaylist(McpWorld.CardId, title));

        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public async Task RenamePlaylist_TitleOverYotoLimit_ThrowsAndSavesNothing()
    {
        var world = new McpWorld();
        var title = new string('a', TitleCleaner.MaxTitleLength + 1);

        var error = await Assert.ThrowsAsync<McpException>(() => world.PlaylistEdit.RenamePlaylist(McpWorld.CardId, title));

        Assert.Contains("140", error.Message, StringComparison.Ordinal);
        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public async Task RenamePlaylist_UnknownId_ThrowsAndSavesNothing()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.PlaylistEdit.RenamePlaylist("nope", "New"));

        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public void RenamePlaylist_Description_StatesTheCurrentTitleLimit()
    {
        var description = typeof(PlaylistEditTools).GetMethod(nameof(PlaylistEditTools.RenamePlaylist))!.GetCustomAttribute<DescriptionAttribute>()!.Description;

        Assert.Contains($"at most {TitleCleaner.MaxTitleLength} characters", description, StringComparison.Ordinal);
    }
}
