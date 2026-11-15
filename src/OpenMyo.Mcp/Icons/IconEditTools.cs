using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenMyo.Core.Icons;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Icons;

/// <summary>Sets chosen icons on tracks. Changes are saved straight to Yoto.</summary>
[McpServerToolType]
public sealed class IconEditTools(PlaylistLookup lookup, IconResolver resolver, IconLabeler labeler)
{
    [McpServerTool(Name = "set_track_icons", Destructive = false, Idempotent = true)]
    [Description("Sets the icon on specific tracks and saves the playlist to Yoto immediately. Validates everything first (all-or-nothing). Returns {saved, previous:[{track, icon, title}]}; to undo, call again with the previous ids (tracks that had no icon cannot be cleared).")]
    public Task<SetIconsResult> SetTrackIcons(
        [Description("Playlist id from list_playlists.")] string id,
        [Description("Track/icon pairs, e.g. [{\"track\":3,\"icon\":\"abc123\"}].")] IReadOnlyList<IconAssignmentInput> assignments,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            if (assignments is null || assignments.Count == 0)
            {
                throw new McpException("Provide at least one {track, icon} assignment.");
            }

            var playlist = await lookup.LoadAsync(id, cancellationToken);
            TrackRange.Validate(playlist, assignments.Select(item => item.Track));
            var mediaIds = await resolver.ResolveAsync(assignments.Select(item => item.Icon), cancellationToken);
            var previous = await labeler.GetCurrentIconsAsync(playlist, assignments.Select(item => item.Track).Distinct(), cancellationToken);

            var chapters = playlist.Chapters.ToArray();
            foreach (var item in assignments)
            {
                chapters[item.Track - 1] = IconAutoAssigner.WithIcon(chapters[item.Track - 1], mediaIds[item.Icon]);
            }

            await lookup.SaveAsync(playlist with { Chapters = chapters }, cancellationToken);
            return new SetIconsResult(assignments.Select(item => item.Track).Distinct().Count(), previous);
        });
}
