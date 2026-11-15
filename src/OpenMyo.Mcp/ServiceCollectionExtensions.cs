using Microsoft.Extensions.DependencyInjection;
using OpenMyo.Mcp.Icons;
using OpenMyo.Mcp.Players;
using OpenMyo.Mcp.Playlists;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP tool classes and their helpers. Needs the Core services (<c>AddCore</c>) and an
    /// <c>IPlaylistRepository</c>, <c>IPlayerDirectory</c> and <c>IPlayerRemote</c> to be registered as well.
    /// </summary>
    public static IServiceCollection AddMcpTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PlaylistLookup>();
        services.AddSingleton<IconLabeler>();
        services.AddSingleton<IconResolver>();
        services.AddSingleton<PlaylistTools>();
        services.AddSingleton<TrackSearchTools>();
        services.AddSingleton<PlaylistEditTools>();
        services.AddSingleton<TrackEditTools>();
        services.AddSingleton<IconSearchTools>();
        services.AddSingleton<IconEditTools>();
        services.AddSingleton<IconAutomationTools>();
        services.AddSingleton<PlayerTools>();
        return services;
    }
}
