using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Playlists;

/// <summary>Renames tracks. Changes are saved straight to Yoto.</summary>
[McpServerToolType]
public sealed class TrackEditTools(PlaylistLookup lookup)
{
    [McpServerTool(Name = "rename_tracks", Destructive = false, Idempotent = true)]
    [Description("Renames specific tracks and saves the playlist to Yoto immediately. Validates everything first (all-or-nothing). Titles are trimmed, must not be empty and may be at most 140 characters; each track may appear once. Returns {saved, previous:[{track, title}]}; to undo, call again with the previous titles.")]
    public Task<RenameTracksResult> RenameTracks(
        [Description("Playlist id from list_playlists.")] string id,
        [Description("Track/title pairs, e.g. [{\"track\":3,\"title\":\"The Gruffalo\"}].")] IReadOnlyList<TrackRenameInput> renames,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            TrackRenameValidator.Validate(renames);

            var playlist = await lookup.LoadAsync(id, cancellationToken);
            TrackRange.Validate(playlist, renames.Select(item => item.Track));
            var tracks = renames.Select(item => item.Track).ToList();
            var previous = tracks.Select(track => new PreviousTitle(track, playlist.Chapters[track - 1].Title)).ToList();

            var chapters = playlist.Chapters.ToArray();
            foreach (var item in renames)
            {
                chapters[item.Track - 1] = chapters[item.Track - 1].WithTitle(item.Title);
            }

            await lookup.SaveAsync(playlist with { Chapters = chapters }, cancellationToken);
            return new RenameTracksResult(tracks.Count, previous);
        });

}
