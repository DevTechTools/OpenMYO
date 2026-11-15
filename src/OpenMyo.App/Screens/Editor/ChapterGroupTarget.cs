using OpenMyo.App.Screens.IconPicker;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.Editor;

/// <summary>Several selected tracks treated as one icon target: one click gives them all the same icon.</summary>
public sealed class ChapterGroupTarget(IReadOnlyList<ChapterRowViewModel> rows) : IIconTarget
{
    public string Title => $"{rows.Count} selected tracks";

    /// <summary>The first track's title is the most useful starting search.</summary>
    public string SearchText => IconQuery.FromTitle(rows[0].Title);

    public bool HasIcon => rows.All(row => row.HasIcon);

    public void AssignIcon(DisplayIcon icon)
    {
        foreach (var row in rows)
        {
            row.AssignIcon(icon);
        }
    }
}
