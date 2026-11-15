using System.Collections.ObjectModel;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenMyo.App.Screens.IconPicker;

namespace OpenMyo.App.Screens.Editor;

/// <summary>
/// Which tracks are selected in the editor, and the icon target that selection stands for: one track gets
/// its own icon; several tracks get the same icon in one click. The editor points its picker at it.
/// </summary>
public sealed partial class ChapterSelection : ObservableObject
{
    private readonly ObservableCollection<ChapterRowViewModel> _chapters;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMany))]
    private int _count;

    public ChapterSelection(ObservableCollection<ChapterRowViewModel> chapters)
    {
        _chapters = chapters;
        Model.Source = chapters;
        Model.SelectionChanged += (_, _) => OnSelectionChanged();
    }

    /// <summary>Raised whenever the selected tracks change.</summary>
    public event EventHandler? Changed;

    public SelectionModel<ChapterRowViewModel> Model { get; } = new() { SingleSelect = false };

    public bool HasMany => Count > 1;

    public IReadOnlyList<ChapterRowViewModel> Rows => Model.SelectedItems.OfType<ChapterRowViewModel>().ToList();

    /// <summary>Positions of the selected rows, in playlist order.</summary>
    public IReadOnlyList<int> Indexes => Model.SelectedIndexes.Order().ToList();

    /// <summary>What an icon choice applies to: nothing, one track, or every selected track.</summary>
    public IIconTarget? Target => Rows switch
    {
        [] => null,
        [var row] => row,
        var rows => new ChapterGroupTarget(rows),
    };

    /// <summary>The 1-based position of the single selected track; null unless exactly one is selected.</summary>
    public int? TrackNumber => Rows is [var row] ? _chapters.IndexOf(row) + 1 : null;

    /// <summary>Selects the first track that still needs an icon, so the picker is ready for it.</summary>
    public void SelectFirstWithoutIcon() => Select(IconTargetSequence.NextWithoutIcon(_chapters, null) ?? _chapters.FirstOrDefault());

    /// <summary>After one track got its icon, moves on to the next track without one: one click per icon.</summary>
    public void SelectNextWithoutIcon()
    {
        if (Rows is [var current] && IconTargetSequence.NextWithoutIcon(_chapters, current) is { } next)
        {
            Select(next);
        }
    }

    /// <summary>Selects just <paramref name="row"/>, or nothing when it is null.</summary>
    public void Select(ChapterRowViewModel? row)
    {
        Model.Clear();
        if (row is not null)
        {
            Model.Select(_chapters.IndexOf(row));
        }
    }

    /// <summary>Selects the track with <paramref name="chapterKey"/>; the list scrolls it into view.</summary>
    public void SelectKey(string chapterKey) =>
        Select(_chapters.FirstOrDefault(row => string.Equals(row.Key, chapterKey, StringComparison.Ordinal)));

    private void OnSelectionChanged()
    {
        Count = Model.SelectedItems.Count;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
