using ModelContextProtocol;
using OpenMyo.Mcp.Icons;

namespace OpenMyo.Mcp.Tests.Icons;

public sealed class IconEditToolsTests
{
    [Fact]
    public async Task SetTrackIcons_ValidAssignment_SavesAndReturnsPreviousIcon()
    {
        var world = new McpWorld();

        var result = await world.Edit.SetTrackIcons(McpWorld.CardId, [new IconAssignmentInput(1, "rabbit-id")]);

        var saved = Assert.Single(world.Repository.Saved);
        Assert.Equal("rabbit-id", saved.Chapters[0].IconMediaId);
        Assert.Equal("rabbit-id", saved.Chapters[0].Tracks[0].IconMediaId);
        Assert.Equal(1, result.Saved);
        Assert.Equal("moon-id", Assert.Single(result.Previous).Icon);
    }

    [Fact]
    public async Task SetTrackIcons_TrackOutOfRange_ThrowsAndSavesNothing()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.Edit.SetTrackIcons(
            McpWorld.CardId, [new IconAssignmentInput(2, "rabbit-id"), new IconAssignmentInput(9, "moon-id")]));

        Assert.Empty(world.Repository.Saved);
    }

    [Fact]
    public async Task SetTrackIcons_UnknownIcon_ThrowsListingItAndSavesNothing()
    {
        var world = new McpWorld();

        var error = await Assert.ThrowsAsync<McpException>(() => world.Edit.SetTrackIcons(
            McpWorld.CardId, [new IconAssignmentInput(1, "rabbit-id"), new IconAssignmentInput(2, "bogus")]));

        Assert.Contains("bogus", error.Message, StringComparison.Ordinal);
        Assert.Empty(world.Repository.Saved);
        Assert.Empty(world.Uploader.IconNames);
    }

    [Fact]
    public async Task SetTrackIcons_CommunityIcon_ImportsItAndAssignsUploadedId()
    {
        var world = new McpWorld(community: [McpWorld.Community("77", "rabbit hole")]);

        await world.Edit.SetTrackIcons(McpWorld.CardId, [new IconAssignmentInput(2, "yotoicons:77")]);

        Assert.Single(world.Uploader.IconNames);
        Assert.Equal("uploaded-1", Assert.Single(world.Repository.Saved).Chapters[1].IconMediaId);
    }

    [Fact]
    public async Task SetTrackIcons_UnknownCommunityIcon_ThrowsWithoutUploading()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<McpException>(() => world.Edit.SetTrackIcons(McpWorld.CardId, [new IconAssignmentInput(1, "yotoicons:404")]));

        Assert.Empty(world.Uploader.IconNames);
    }
}
