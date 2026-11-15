using ModelContextProtocol;
using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Icons;

namespace OpenMyo.Mcp.Icons;

/// <summary>
/// Turns the icon ids Claude chose into Yoto media ids: checks every id first, and only then imports community
/// icons, so one bad id never leaves a half-done change.
/// </summary>
public sealed class IconResolver(IconIndexProvider indexProvider, CommunityIconSearch community, CommunityIconImporter importer)
{
    /// <summary>Maps each distinct icon id to a media id.</summary>
    /// <exception cref="McpException">One or more ids are unknown; nothing is imported.</exception>
    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(IEnumerable<string> iconIds, CancellationToken cancellationToken)
    {
        var index = await indexProvider.GetAsync(cancellationToken);
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        var pending = new Dictionary<string, CommunityIcon>(StringComparer.Ordinal);
        var unknown = new List<string>();
        foreach (var id in iconIds.Distinct(StringComparer.Ordinal))
        {
            if (!id.StartsWith(IconSearchTools.CommunityPrefix, StringComparison.Ordinal))
            {
                AddIfKnown(index.Find(id) is not null, id, resolved, unknown);
                continue;
            }

            var icon = await community.FindAsync(id[IconSearchTools.CommunityPrefix.Length..], cancellationToken);
            if (icon is null)
            {
                unknown.Add(id);
            }
            else
            {
                pending[id] = icon;
            }
        }

        if (unknown.Count > 0)
        {
            throw new McpException($"Unknown icon id(s): {string.Join(", ", unknown)}. Nothing was changed; use ids from search_icons.");
        }

        foreach (var (id, icon) in pending)
        {
            resolved[id] = (await importer.ImportAsync(icon, cancellationToken)).MediaId;
        }

        return resolved;
    }

    private static void AddIfKnown(bool known, string id, Dictionary<string, string> resolved, List<string> unknown)
    {
        if (known)
        {
            resolved[id] = id;
        }
        else
        {
            unknown.Add(id);
        }
    }
}
