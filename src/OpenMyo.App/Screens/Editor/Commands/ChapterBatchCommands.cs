using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.Editor.Commands;

/// <summary>
/// Edits that apply to many tracks at once: the suggestion fixes, the selection bar and the ⋯ menu.
/// Nothing is saved until the user presses Save.
/// </summary>
public sealed partial class ChapterBatchCommands(
    ObservableCollection<ChapterRowViewModel> chapters, ChapterSelection selection, Action markDirty)
{
    [RelayCommand]
    private void ClearAllIcons() => ClearIcons(chapters);

    [RelayCommand]
    private void ClearSelectedIcons() => ClearIcons(selection.Rows);

    [RelayCommand]
    private void RemoveSelected()
    {
        foreach (var row in selection.Rows)
        {
            chapters.Remove(row);
        }

        markDirty();
    }

    [RelayCommand]
    private void SortByTitle() =>
        Reorder(chapters.OrderBy(chapter => chapter.Title, NaturalStringComparer.Instance).ToList());

    [RelayCommand]
    private void SortByNumber() => Reorder([.. TitleNumbers.SortByNumber(chapters.ToList(), chapter => chapter.Title)]);

    [RelayCommand]
    private void ReverseOrder() => Reorder(chapters.Reverse().ToList());

    private static void ClearIcons(IEnumerable<ChapterRowViewModel> rows)
    {
        foreach (var row in rows.ToList())
        {
            row.IconMediaId = null;
        }
    }

    private void Reorder(List<ChapterRowViewModel> ordered)
    {
        if (ordered.SequenceEqual(chapters))
        {
            return;
        }

        for (var target = 0; target < ordered.Count; target++)
        {
            chapters.Move(chapters.IndexOf(ordered[target]), target);
        }

        markDirty();
    }
}
