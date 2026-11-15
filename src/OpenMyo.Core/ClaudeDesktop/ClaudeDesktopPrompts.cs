namespace OpenMyo.Core.ClaudeDesktop;

/// <summary>
/// Ready-to-paste requests for Claude Desktop, used when the user has no API key but has connected OpenMyo as a
/// connector. Each names the playlist by title and id so Claude acts on the right one without asking.
/// </summary>
public static class ClaudeDesktopPrompts
{
    /// <param name="replaceExisting">True to re-pick every icon; false to fill in only tracks without one.</param>
    public static string PickIcons(string playlistTitle, string? playlistId, bool replaceExisting)
    {
        var playlist = Describe(playlistTitle, playlistId);
        var task = replaceExisting
            ? $"Use OpenMyo to re-pick the icon for every track in {playlist}. Start with auto_match_icons with replaceExisting set to true"
            : $"Use OpenMyo to pick icons for the tracks without one in {playlist}. Start with auto_match_icons";
        return task + ", then use search_icons to improve any weak matches and fill in tracks it left without an icon.";
    }

    /// <summary>Asks Claude to give every track a clean display title with rename_tracks.</summary>
    public static string RenameTracks(string playlistTitle, string? playlistId) =>
        $"Use OpenMyo to rename every track in {Describe(playlistTitle, playlistId)}. Call get_playlist, then give each " +
        "track a clean, short display title: drop track numbers, file-name leftovers and text repeating the playlist " +
        "name, fix capitalisation, and keep the real name of the song, story or chapter. Leave titles that are already " +
        "good, then save the rest in one rename_tracks call.";

    private static string Describe(string playlistTitle, string? playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistTitle);
        return playlistId is { Length: > 0 }
            ? $"my Yoto playlist \"{playlistTitle}\" (id {playlistId})"
            : $"my Yoto playlist \"{playlistTitle}\"";
    }
}
