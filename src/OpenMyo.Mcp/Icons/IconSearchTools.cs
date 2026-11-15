using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Icons;
using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Icons;

/// <summary>Searches Yoto's icons, the user's own uploads and the yotoicons.com community catalog.</summary>
[McpServerToolType]
public sealed class IconSearchTools(IconIndexProvider indexProvider, CommunityIconSearch community)
{
    /// <summary>Id prefix that marks a yotoicons.com icon; set_track_icons imports it on demand.</summary>
    public const string CommunityPrefix = "yotoicons:";

    private const int MaxLimit = 30;
    private const string CommunitySource = "yotoicons.com";

    [McpServerTool(Name = "search_icons", ReadOnly = true)]
    [Description("Searches 16x16 track icons by subject words (e.g. 'rabbit foot'). Returns {id, title, source, artist?}; source is 'yoto' (Yoto's library), 'mine' (the user's uploads) or 'yotoicons.com' (community, ids start with 'yotoicons:'; imported when assigned). Try several short, concrete queries or synonyms.")]
    public Task<IconSearchResult> SearchIcons(
        [Description("Subject words to search for.")] string query,
        [Description("Maximum results per source (1-30, default 12).")] int limit = 12,
        CancellationToken cancellationToken = default) =>
        ToolGuard.RunAsync(async () =>
        {
            var max = Math.Clamp(limit, 1, MaxLimit);
            var index = await indexProvider.GetAsync(cancellationToken);
            var hits = index.Search(query, max).Select(match => ToHit(match.Icon)).ToList();
            var (extra, note) = await SearchCommunityAsync(query, max, cancellationToken);
            return new IconSearchResult([.. hits, .. extra], note);
        });

    private async Task<(IReadOnlyList<IconHit> Hits, string? Note)> SearchCommunityAsync(
        string query, int max, CancellationToken cancellationToken)
    {
        try
        {
            var found = await community.SearchAsync(query, max, cancellationToken);
            return (found.Select(ToHit).ToList(), null);
        }
        catch (HttpRequestException)
        {
            return ([], "yotoicons.com is unreachable right now; only Yoto and personal icons are shown.");
        }
    }

    private static IconHit ToHit(DisplayIcon icon) => new(icon.MediaId, icon.Title, icon.IsPublic ? "yoto" : "mine", null);

    private static IconHit ToHit(CommunityIcon icon) => new(CommunityPrefix + icon.Id, icon.Title, CommunitySource, icon.Artist);
}
