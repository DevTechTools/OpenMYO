namespace OpenMyo.Mcp.Tests.Playlists;

public sealed class TrackSearchToolsTests
{
    [Fact]
    public async Task SearchTracks_WordInTrackTitle_ReturnsPlaylistAndOneBasedIndex()
    {
        var world = new McpWorld();

        var hit = Assert.Single(await world.TrackSearch.SearchTracks("moon"));

        Assert.Equal((McpWorld.CardId, "Bedtime", 2, "Chapter 2 - The Moon"), (hit.PlaylistId, hit.PlaylistTitle, hit.TrackIndex, hit.Title));
    }

    [Fact]
    public async Task SearchTracks_NoMatch_ReturnsEmpty()
    {
        var world = new McpWorld();

        Assert.Empty(await world.TrackSearch.SearchTracks("zebra"));
    }

    [Fact]
    public async Task SearchTracks_BlankQuery_ThrowsActionableError()
    {
        var world = new McpWorld();

        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() => world.TrackSearch.SearchTracks("  "));
    }
}
