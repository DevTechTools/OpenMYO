namespace OpenMyo.Mcp.Tests.Playlists;

public sealed class PlaylistToolsTests
{
    [Fact]
    public async Task ListPlaylists_SearchGiven_FiltersByTitleIgnoringCase()
    {
        var world = new McpWorld();

        var hit = await world.Playlists.ListPlaylists("BED");
        var miss = await world.Playlists.ListPlaylists("odyssey");

        Assert.Equal(McpWorld.CardId, Assert.Single(hit).Id);
        Assert.Empty(miss);
    }

    [Fact]
    public async Task GetPlaylist_Tracks_MapIconTitleAndSearchQuery()
    {
        var world = new McpWorld();

        var view = await world.Playlists.GetPlaylist(McpWorld.CardId);

        Assert.Equal(2, view.Tracks.Count);
        Assert.Equal(1, view.Tracks[0].Index);
        Assert.Equal("moon-id", view.Tracks[0].Icon?.Id);
        Assert.Equal("moon", view.Tracks[0].Icon?.Title);
        Assert.Equal("Rabbit", view.Tracks[0].Query);
        Assert.Null(view.Tracks[1].Icon);
    }

    [Fact]
    public async Task GetPlaylist_UnknownId_ThrowsActionableError()
    {
        var world = new McpWorld();

        var error = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() => world.Playlists.GetPlaylist("nope"));

        Assert.Contains("list_playlists", error.Message, StringComparison.Ordinal);
    }
}
