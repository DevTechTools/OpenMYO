using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Players;

/// <summary>A Yoto player as list_players shows it.</summary>
public sealed record PlayerView(string Id, string Name, bool Online);

/// <summary>What a player command did, in words Claude can relay.</summary>
public sealed record PlayerResult(string Player, string Done);

/// <summary>
/// Plays and controls tracks on the user's Yoto players. Each call connects, sends one command and disconnects:
/// Yoto allows one control connection per player, and the OpenMyo app may need it too.
/// </summary>
[McpServerToolType]
public sealed class PlayerTools(IPlayerDirectory directory, IPlayerRemote remote, PlaylistLookup lookup)
{
    [McpServerTool(Name = "list_players", ReadOnly = true)]
    [Description("Lists the user's Yoto players as {id, name, online}. Only online players can play.")]
    public Task<IReadOnlyList<PlayerView>> ListPlayers(CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () => (IReadOnlyList<PlayerView>)(await directory.ListAsync(cancellationToken))
            .Select(player => new PlayerView(player.DeviceId, player.Name, player.IsOnline))
            .ToList());

    [McpServerTool(Name = "play_track", Destructive = false, OpenWorld = true)]
    [Description("Starts a playlist on a Yoto player at the given track; it then plays on through the playlist. Audio plays out loud in the room where that player is, so only call this when the user asked to play something.")]
    public Task<PlayerResult> PlayTrack(
        [Description("Playlist id from list_playlists or search_tracks.")] string playlistId,
        [Description("1-based track index, as get_playlist and search_tracks show it.")] int trackIndex,
        [Description("Player name or id from list_players.")] string player,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var playlist = await lookup.LoadAsync(playlistId, cancellationToken);
            TrackRange.Validate(playlist, [trackIndex]);
            var chapter = playlist.Chapters[trackIndex - 1];
            var target = PlayTargets.ForChapter(playlist, chapter.Key, TimeSpan.Zero)
                ?? throw new McpException($"Track {trackIndex} has no audio to play.");
            var found = await FindAsync(player, cancellationToken);
            await SendAsync(found, () => remote.PlayAsync(found.DeviceId, target, cancellationToken), cancellationToken);
            return new PlayerResult(found.Name, $"Playing “{chapter.Title}” from “{playlist.Title}”.");
        });

    [McpServerTool(Name = "control_player", Destructive = false, OpenWorld = true)]
    [Description("Pauses, resumes or stops a Yoto player, and/or sets its volume in player steps (0-16; the player caps this at its parental limit).")]
    public Task<PlayerResult> ControlPlayer(
        [Description("Player name or id from list_players.")] string player,
        [Description("'pause', 'resume' or 'stop'; omit to change only the volume.")] string? action = null,
        [Description("Volume step 0-16 (about 8 is medium); omit to leave it unchanged.")] int? volume = null,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var command = ParseAction(action);
            if (command is null && volume is null)
            {
                throw new McpException("Give an action ('pause', 'resume' or 'stop') or a volume.");
            }

            var found = await FindAsync(player, cancellationToken);
            await SendAsync(found, async () =>
            {
                if (command is { } chosen)
                {
                    await remote.SendAsync(found.DeviceId, chosen, cancellationToken);
                }

                if (volume is { } level)
                {
                    await remote.SetVolumeAsync(found.DeviceId, level, cancellationToken);
                }
            }, cancellationToken);
            return new PlayerResult(found.Name, string.Join(" and ", Describe(command, volume)) + ".");
        });

    private async Task<YotoPlayer> FindAsync(string player, CancellationToken cancellationToken)
    {
        var players = await directory.ListAsync(cancellationToken);
        var found = players.FirstOrDefault(item => string.Equals(item.DeviceId, player, StringComparison.Ordinal))
            ?? players.FirstOrDefault(item => string.Equals(item.Name, player.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new McpException($"No Yoto player called '{player}'. Call list_players to see their names.");
        return found.IsOnline
            ? found
            : throw new McpException($"{found.Name} is offline. Ask the user to switch it on and connect it to Wi-Fi.");
    }

    private async Task SendAsync(YotoPlayer player, Func<Task> commands, CancellationToken cancellationToken)
    {
        await remote.ConnectAsync(player.DeviceId, cancellationToken);
        try
        {
            await commands();
        }
        finally
        {
            await remote.DisconnectAsync(player.DeviceId, CancellationToken.None);
        }
    }

    private static PlayerCommand? ParseAction(string? action) => action?.Trim().ToUpperInvariant() switch
    {
        null or "" => null,
        "PAUSE" => PlayerCommand.Pause,
        "RESUME" or "PLAY" => PlayerCommand.Resume,
        "STOP" => PlayerCommand.Stop,
        _ => throw new McpException($"Unknown action '{action}'. Use 'pause', 'resume' or 'stop'."),
    };

    private static IEnumerable<string> Describe(PlayerCommand? command, int? volume)
    {
        if (command is { } chosen)
        {
            yield return chosen switch
            {
                PlayerCommand.Pause => "Paused",
                PlayerCommand.Resume => "Resumed",
                _ => "Stopped",
            };
        }

        if (volume is { } level)
        {
            yield return $"Volume set to {Math.Clamp(level, 0, PlayerState.VolumeSteps)} of {PlayerState.VolumeSteps}";
        }
    }
}
