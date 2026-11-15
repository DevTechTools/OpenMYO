using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;
using OpenMyo.Core.Text;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Icons;

/// <summary>Bulk icon tools that use OpenMyo's own matcher; Claude handles whatever they leave over.</summary>
[McpServerToolType]
public sealed class IconAutomationTools(PlaylistLookup lookup, IconAutoAssigner assigner, IconLabeler labeler, IconIndexProvider indexProvider)
{
    [McpServerTool(Name = "auto_match_icons", Destructive = false)]
    [Description("Runs OpenMyo's free automatic icon matcher (Yoto library plus yotoicons.com) and saves the result to Yoto. By default only tracks without an icon change. Returns the tracks it changed (with the old icon id in 'was') and 'stillMissing' tracks you can handle with search_icons and set_track_icons.")]
    public Task<AutoMatchResult> AutoMatchIcons(
        [Description("Playlist id from list_playlists.")] string id,
        [Description("Also replace icons tracks already have.")] bool replaceExisting = false,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var playlist = await lookup.LoadAsync(id, cancellationToken);
            var assigned = await assigner.MatchIconsAsync(playlist, replaceExisting, useAi: false, cancellationToken);
            var updated = assigned.Playlist;
            var changed = await DescribeChangesAsync(playlist, updated, cancellationToken);
            if (changed.Count > 0)
            {
                await lookup.SaveAsync(updated, cancellationToken);
            }

            return new AutoMatchResult(changed, MissingTracks(updated));
        });

    [McpServerTool(Name = "number_track_icons", Destructive = false)]
    [Description("Gives tracks the icon showing their position (track 3 gets '3') and saves to Yoto. Omit tracks to number every track. Returns {saved, previous:[...]} so the change can be undone; numbers above 999 are skipped.")]
    public Task<SetIconsResult> NumberTrackIcons(
        [Description("Playlist id from list_playlists.")] string id,
        [Description("Optional 1-based track numbers; default all.")] IReadOnlyList<int>? tracks = null,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var playlist = await lookup.LoadAsync(id, cancellationToken);
            var numbers = tracks is { Count: > 0 } ? tracks.Distinct().ToList() : Enumerable.Range(1, playlist.Chapters.Count).ToList();
            TrackRange.Validate(playlist, numbers);
            var previous = await labeler.GetCurrentIconsAsync(playlist, numbers, cancellationToken);
            var result = await assigner.AssignNumberIconsAsync(playlist, numbers.Select(number => number - 1).ToList(), cancellationToken);
            await lookup.SaveAsync(result.Playlist, cancellationToken);
            return new SetIconsResult(numbers.Count - result.MissingNumbers.Count, previous);
        });

    private async Task<List<MatchedTrack>> DescribeChangesAsync(Playlist before, Playlist after, CancellationToken cancellationToken)
    {
        var index = await indexProvider.GetAsync(cancellationToken);
        var changed = new List<MatchedTrack>();
        for (var i = 0; i < before.Chapters.Count; i++)
        {
            var now = after.Chapters[i];
            if (now.IconMediaId is null || now.IconMediaId == before.Chapters[i].IconMediaId)
            {
                continue;
            }

            // Auto-matching only ever picks Yoto's public icons or imports yotoicons.com ones.
            var source = index.Find(now.IconMediaId)?.IsPublic == true ? "yoto" : "yotoicons.com";
            var reference = await labeler.DescribeIconAsync(now.IconMediaId, cancellationToken) ?? new IconRef(now.IconMediaId, null);
            changed.Add(new MatchedTrack(i + 1, now.Title, reference, source, before.Chapters[i].IconMediaId));
        }

        return changed;
    }

    private static List<MissingTrack> MissingTracks(Playlist playlist)
    {
        var series = SeriesWords.Find(playlist.Chapters.Select(chapter => chapter.Title).ToList());
        return playlist.Chapters
            .Select((chapter, i) => (chapter, i))
            .Where(pair => !pair.chapter.HasIcon)
            .Select(pair => new MissingTrack(pair.i + 1, pair.chapter.Title, IconQuery.FromTitle(pair.chapter.Title, series)))
            .ToList();
    }
}
