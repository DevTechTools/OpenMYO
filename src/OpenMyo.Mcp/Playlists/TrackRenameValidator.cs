using ModelContextProtocol;
using OpenMyo.Core.Text;

namespace OpenMyo.Mcp.Playlists;

/// <summary>
/// Checks rename_tracks input before anything is loaded or saved, so Claude gets one clear error naming every
/// bad track instead of a Yoto API failure halfway through.
/// </summary>
public static class TrackRenameValidator
{
    /// <summary>Throws an <see cref="McpException"/> for empty input, repeated tracks, or titles Yoto would reject.</summary>
    public static void Validate(IReadOnlyList<TrackRenameInput>? renames)
    {
        if (renames is null || renames.Count == 0)
        {
            throw new McpException("Provide at least one {track, title} rename.");
        }

        var repeated = renames.GroupBy(item => item.Track).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Fail(repeated, "appear more than once; rename each track once per call");

        var blank = renames.Where(item => string.IsNullOrWhiteSpace(item.Title)).Select(item => item.Track).ToList();
        Fail(blank, "have an empty title");

        var tooLong = renames.Where(item => item.Title?.Trim().Length > TitleCleaner.MaxTitleLength).Select(item => item.Track).ToList();
        Fail(tooLong, $"have titles longer than {TitleCleaner.MaxTitleLength} characters, which Yoto rejects");
    }

    private static void Fail(List<int> tracks, string problem)
    {
        if (tracks.Count > 0)
        {
            throw new McpException($"Track(s) {string.Join(", ", tracks)} {problem}.");
        }
    }
}
