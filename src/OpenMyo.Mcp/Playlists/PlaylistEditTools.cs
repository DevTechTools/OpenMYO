using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenMyo.Core.Text;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Playlists;

/// <summary>Changes a playlist's own details. Changes are saved straight to Yoto.</summary>
[McpServerToolType]
public sealed class PlaylistEditTools(PlaylistLookup lookup)
{
    [McpServerTool(Name = "rename_playlist", Destructive = false, Idempotent = true)]
    [Description("Changes a playlist's title and saves it to Yoto immediately. The title is trimmed, must not be empty and may be at most 140 characters. Track titles are not touched (use rename_tracks). Returns {title, previous}; to undo, call again with the previous title.")]
    public Task<RenamePlaylistResult> RenamePlaylist(
        [Description("Playlist id from list_playlists.")] string id,
        [Description("The new playlist title.")] string title,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var trimmed = title?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                throw new McpException("Provide a non-empty playlist title.");
            }

            if (trimmed.Length > TitleCleaner.MaxTitleLength)
            {
                throw new McpException($"The title is {trimmed.Length} characters; Yoto allows at most {TitleCleaner.MaxTitleLength}.");
            }

            var playlist = await lookup.LoadAsync(id, cancellationToken);
            await lookup.SaveAsync(playlist with { Title = trimmed }, cancellationToken);
            return new RenamePlaylistResult(trimmed, playlist.Title);
        });
}
