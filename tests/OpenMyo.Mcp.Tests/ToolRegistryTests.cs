using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void Build_AllToolClasses_ExposesTheTwelveSnakeCaseTools()
    {
        var tools = ToolRegistry.Build(new ServiceProviderStub());

        Assert.Equal(
            ["auto_match_icons", "control_player", "get_playlist", "list_players", "list_playlists", "number_track_icons", "play_track", "rename_playlist", "rename_tracks", "search_icons", "search_tracks", "set_track_icons"],
            tools.PrimitiveNames.Order(StringComparer.Ordinal));
    }

    private sealed class ServiceProviderStub : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
