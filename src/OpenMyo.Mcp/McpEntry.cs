using ModelContextProtocol.Server;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp;

/// <summary>Entry point the app calls when Claude Desktop launches it with <c>--mcp</c>.</summary>
public static class McpEntry
{
    private const string ServerName = "openmyo";
    private const string ServerTitle = "OpenMyo";

    private const string Instructions =
        "Manage playlist titles, track titles and icons on the user's Yoto Make Your Own playlists. Workflow: list_playlists to find the playlist, " +
        "get_playlist to see each track's title, current icon and a suggested search 'query'. For a quick start call " +
        "auto_match_icons, then handle 'stillMissing' tracks (and weak matches) yourself: search_icons with short " +
        "concrete subject words (try synonyms), pick the best fit, then set_track_icons. number_track_icons gives " +
        "tracks number icons. rename_tracks changes track titles; rename_playlist changes the playlist title. Changes save straight to Yoto; results include previous icons/titles so you can undo. " +
        "search_tracks finds a track by title across every playlist. To play one on a Yoto player: list_players, then play_track " +
        "(only when the user asks; it plays out loud in that room). control_player pauses, resumes, stops or sets the volume.";

    /// <summary>
    /// Serves MCP over stdio until the client disconnects or <paramref name="cancellationToken"/> is cancelled.
    /// Only JSON-RPC may be written to stdout, so the caller must not write anything else there.
    /// </summary>
    /// <param name="services">The app's service provider, with the Core services and <c>AddMcpTools</c> registered.</param>
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new McpServerOptions
        {
            ServerInfo = new() { Name = ServerName, Title = ServerTitle, Version = ServerVersion(), Icons = ServerIcons.Load() },
            ServerInstructions = Instructions,
            ToolCollection = ToolRegistry.Build(services),
        };
        await using var transport = new StdioServerTransport(options);
        await using var server = McpServer.Create(transport, options, loggerFactory: null, services);
        await server.RunAsync(cancellationToken);
    }

    private static string ServerVersion() =>
        typeof(McpEntry).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
}
