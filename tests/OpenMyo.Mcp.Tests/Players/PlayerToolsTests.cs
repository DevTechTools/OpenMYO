namespace OpenMyo.Mcp.Tests.Players;

public sealed class PlayerToolsTests
{
    [Fact]
    public async Task PlayTrack_OnlinePlayerByName_ConnectsPlaysThenDisconnects()
    {
        var world = new McpWorld();

        var result = await world.Players.PlayTrack(McpWorld.CardId, 2, "kitchen");

        Assert.Equal(["connect d1", $"d1 play {McpWorld.CardId}#2@0", "disconnect d1"], world.Remote.Log);
        Assert.Equal("Kitchen", result.Player);
    }

    [Fact]
    public async Task PlayTrack_OfflinePlayer_ThrowsWithoutConnecting()
    {
        var world = new McpWorld();

        var error = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() => world.Players.PlayTrack(McpWorld.CardId, 1, "Bedroom"));

        Assert.Contains("offline", error.Message, StringComparison.Ordinal);
        Assert.Empty(world.Remote.Log);
    }

    [Fact]
    public async Task PlayTrack_TrackOutOfRange_ThrowsActionableError()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() => world.Players.PlayTrack(McpWorld.CardId, 9, "d1"));
    }

    [Fact]
    public async Task ControlPlayer_PauseAndVolume_SendsBoth()
    {
        var world = new McpWorld();

        var result = await world.Players.ControlPlayer("d1", "pause", 8);

        Assert.Equal(["connect d1", "d1 pause", "d1 volume 8", "disconnect d1"], world.Remote.Log);
        Assert.Equal("Paused and Volume set to 8 of 16.", result.Done);
    }

    [Fact]
    public async Task ControlPlayer_NothingToDo_Throws()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() => world.Players.ControlPlayer("d1"));
    }

    [Fact]
    public async Task ListPlayers_Directory_MapsOnline()
    {
        var world = new McpWorld();

        var players = await world.Players.ListPlayers();

        Assert.Equal([new("d1", "Kitchen", true), new("d2", "Bedroom", false)], players);
    }
}
