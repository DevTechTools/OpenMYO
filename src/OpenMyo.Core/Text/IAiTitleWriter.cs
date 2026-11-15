namespace OpenMyo.Core.Text;

/// <summary>
/// Optional AI step: rewrites messy track titles ("03_the_gruffalo_(read_by_X)_192kbps") into clean display
/// titles ("The Gruffalo"). Runs only when the user clicks.
/// </summary>
public interface IAiTitleWriter
{
    /// <summary>
    /// Returns one suggested title per input title, in the same order. An entry may be null or blank when the
    /// writer has no better title; callers keep the original then.
    /// </summary>
    Task<IReadOnlyList<string?>> RewriteAsync(string playlistTitle, IReadOnlyList<string> trackTitles, CancellationToken cancellationToken);
}
