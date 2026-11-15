using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol;
using OpenMyo.Core.Text;
using OpenMyo.Mcp.Playlists;

namespace OpenMyo.Mcp.Tests.Playlists;

public sealed class TrackEditToolsTests
{
    [Fact]
    public async Task RenameTracks_ValidRename_SavesTrimmedTitleOnChapterAndTrack()
    {
        var world = new McpWorld();

        var result = await world.Tracks.RenameTracks(McpWorld.CardId, [new TrackRenameInput(2, "  The Moon  ")]);

        var saved = Assert.Single(world.Repository.Saved);
        Assert.Equal("The Moon", saved.Chapters[1].Title);
        Assert.Equal("The Moon", saved.Chapters[1].Tracks[0].Title);
        Assert.Equal("Chapter 1 - The Rabbit", saved.Chapters[0].Title);
        Assert.Equal(1, result.Saved);
        Assert.Equal(new PreviousTitle(2, "Chapter 2 - The Moon"), Assert.Single(result.Previous));
    }

    [Fact]
    public async Task RenameTracks_TrackOutOfRange_ThrowsAndSavesNothing()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.Tracks.RenameTracks(
            McpWorld.CardId, [new TrackRenameInput(1, "Ok"), new TrackRenameInput(9, "Bad")]));

        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public async Task RenameTracks_BlankTitle_ThrowsNamingTrackAndSavesNothing()
    {
        var world = new McpWorld();

        var error = await Assert.ThrowsAsync<McpException>(() => world.Tracks.RenameTracks(
            McpWorld.CardId, [new TrackRenameInput(1, "Ok"), new TrackRenameInput(2, "   ")]));

        Assert.Contains("2", error.Message, StringComparison.Ordinal);
        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public async Task RenameTracks_NoRenames_Throws()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.Tracks.RenameTracks(McpWorld.CardId, []));
    }
    [Fact]
    public async Task RenameTracks_TitleOverYotoLimit_ThrowsAndSavesNothing()
    {
        var world = new McpWorld();
        var title = new string('a', TitleCleaner.MaxTitleLength + 1);

        var error = await Assert.ThrowsAsync<McpException>(() => world.Tracks.RenameTracks(McpWorld.CardId, [new TrackRenameInput(1, title)]));

        Assert.Contains("140", error.Message, StringComparison.Ordinal);
        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public async Task RenameTracks_TitleAtYotoLimit_Saves()
    {
        var world = new McpWorld();
        var title = new string('a', TitleCleaner.MaxTitleLength);

        await world.Tracks.RenameTracks(McpWorld.CardId, [new TrackRenameInput(1, title)]);

        Assert.Equal(title, Assert.Single(world.Repository.Saved).Chapters[0].Title);
    }

    [Fact]
    public async Task RenameTracks_SameTrackTwice_ThrowsAndSavesNothing()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.Tracks.RenameTracks(
            McpWorld.CardId, [new TrackRenameInput(1, "First"), new TrackRenameInput(1, "Second")]));

        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public void RenameTracks_Description_StatesTheCurrentTitleLimit()
    {
        // Attribute text can't reference an int constant, so this keeps the "140" Claude reads in step with the code.
        var description = typeof(TrackEditTools).GetMethod(nameof(TrackEditTools.RenameTracks))!.GetCustomAttribute<DescriptionAttribute>()!.Description;

        Assert.Contains($"at most {TitleCleaner.MaxTitleLength} characters", description, StringComparison.Ordinal);
    }
}
