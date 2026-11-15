namespace OpenMyo.Mcp.Tests.Icons;

public sealed class IconSearchToolsTests
{
    [Fact]
    public async Task SearchIcons_MatchesInBothSources_MergesWithSourceLabels()
    {
        var world = new McpWorld(community: [McpWorld.Community("77", "rabbit hole")]);

        var result = await world.Search.SearchIcons("rabbit");

        Assert.Contains(result.Icons, hit => hit.Id == "rabbit-id" && hit.Source == "yoto");
        var community = Assert.Single(result.Icons, hit => hit.Source == "yotoicons.com");
        Assert.Equal("yotoicons:77", community.Id);
        Assert.Equal("artist", community.Artist);
        Assert.Null(result.Note);
    }
}
