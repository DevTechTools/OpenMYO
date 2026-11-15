using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Text;

/// <summary>The playlist's chapters after an AI rename, and how many titles actually changed.</summary>
public sealed record AiRenameResult(IReadOnlyList<Chapter> Chapters, int ChangedCount);

/// <summary>
/// Renames every track of a playlist with <see cref="IAiTitleWriter"/>, keeping the original title wherever the
/// suggestion is blank, keeping the original's leading chapter number, and trimming suggestions to Yoto's length limit. Nothing is saved; callers show the
/// result as an unsaved edit.
/// </summary>
public sealed class AiTitleRenamer(IAiTitleWriter? writer = null)
{
    public async Task<AiRenameResult> RenameAllAsync(Playlist playlist, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        if (writer is null)
        {
            throw new InvalidOperationException("AI title renaming is not available.");
        }

        if (playlist.Chapters.Count == 0)
        {
            return new AiRenameResult(playlist.Chapters, 0);
        }

        var titles = playlist.Chapters.Select(chapter => chapter.Title).ToList();
        var suggestions = await writer.RewriteAsync(playlist.Title, titles, cancellationToken);

        var chapters = playlist.Chapters.ToArray();
        var changed = 0;
        for (var i = 0; i < chapters.Length && i < suggestions.Count; i++)
        {
            var suggestion = Fit(suggestions[i], chapters[i].Title);
            if (suggestion is not null && !string.Equals(suggestion, chapters[i].Title, StringComparison.Ordinal))
            {
                chapters[i] = chapters[i].WithTitle(suggestion);
                changed++;
            }
        }

        return new AiRenameResult(chapters, changed);
    }

    private static string? Fit(string? suggestion, string original)
    {
        var trimmed = suggestion?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        // Chapter numbers are the user's ordering; a rewrite that drops them gets them back.
        trimmed = TitleNumberPrefix.Restore(original, trimmed);

        return trimmed.Length <= TitleCleaner.MaxTitleLength ? trimmed : trimmed[..TitleCleaner.MaxTitleLength].TrimEnd();
    }
}
