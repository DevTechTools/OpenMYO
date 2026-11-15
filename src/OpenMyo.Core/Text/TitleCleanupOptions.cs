namespace OpenMyo.Core.Text;

/// <summary>
/// Which clean-up rules <see cref="TitleCleaner"/> applies. Every rule can be switched off, so deliberate
/// styles survive, e.g. "[051] Connie" keeps its bracketed episode number when <see cref="RemoveBracketedText"/> is off.
/// </summary>
public sealed record TitleCleanupOptions
{
    public static TitleCleanupOptions Default { get; } = new();

    /// <summary>
    /// Defaults that respect a playlist's deliberate style: when most titles start with a bracketed tag
    /// ("[051] Connie"), that tag is the user's numbering, not download junk, so it is kept.
    /// </summary>
    public static TitleCleanupOptions SuitedTo(IReadOnlyList<string> titles)
    {
        ArgumentNullException.ThrowIfNull(titles);
        var bracketed = titles.Count(title => title.TrimStart().StartsWith('['));
        return titles.Count > 1 && bracketed * 5 >= titles.Count * 4 ? Default with { RemoveBracketedText = false } : Default;
    }

    /// <summary>Underscores, "%20" and dotted.file.names become spaces; web addresses are removed.</summary>
    public bool FixSeparators { get; init; } = true;

    /// <summary>Leading track and disc numbers: "01 - ", "Track 3.", "CD1 ".</summary>
    public bool RemoveNumbering { get; init; } = true;

    /// <summary>Anything in [square brackets], such as "[x7Gh2]" download ids.</summary>
    public bool RemoveBracketedText { get; init; } = true;

    /// <summary>"(Official Audio)", "(Lyrics)", "(Remastered)" and similar.</summary>
    public bool RemoveNoise { get; init; } = true;

    /// <summary>A "Artist - " prefix repeated on every title.</summary>
    public bool RemoveSharedPrefix { get; init; } = true;

    /// <summary>ALL CAPS or all lower case titles become Title Case.</summary>
    public bool FixCapitals { get; init; } = true;

    /// <summary>Text to replace everywhere (case-insensitive), applied after the rules above. Empty means none.</summary>
    public string Find { get; init; } = string.Empty;

    public string Replace { get; init; } = string.Empty;
}
