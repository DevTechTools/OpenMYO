using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.Editor;

/// <summary>
/// Title clean-up the user stays in control of: each rule is a switch, there is a find/replace, and a live
/// preview shows exactly what will change before anything is applied. Acts on the selected tracks, or all.
/// </summary>
public sealed partial class TitleCleanupViewModel : ObservableObject
{
    private readonly ObservableCollection<ChapterRowViewModel> _chapters;
    private readonly ChapterSelection _selection;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _fixSeparators = true;

    [ObservableProperty]
    private bool _removeNumbering = true;

    [ObservableProperty]
    private bool _removeBracketedText = true;

    [ObservableProperty]
    private bool _removeNoise = true;

    [ObservableProperty]
    private bool _removeSharedPrefix = true;

    [ObservableProperty]
    private bool _fixCapitals = true;

    [ObservableProperty]
    private string _find = string.Empty;

    [ObservableProperty]
    private string _replace = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyPropertyChangedFor(nameof(ApplyText))]
    private int _changeCount;

    [ObservableProperty]
    private string _scopeText = string.Empty;

    public TitleCleanupViewModel(ObservableCollection<ChapterRowViewModel> chapters, ChapterSelection selection)
    {
        (_chapters, _selection) = (chapters, selection);
        PropertyChanged += (_, e) =>
        {
            if (IsOpen && e.PropertyName is not (nameof(ChangeCount) or nameof(ScopeText) or nameof(ApplyText)))
            {
                RefreshPreview();
            }
        };
    }

    public ObservableCollection<TitleChange> Preview { get; } = [];

    public string ApplyText => ChangeCount switch { 0 => "Apply", 1 => "Apply 1 Change", _ => $"Apply {ChangeCount} Changes" };

    /// <summary>Opens with rules suited to the titles, e.g. keeping a deliberate "[051]" numbering style.</summary>
    [RelayCommand]
    private void Open()
    {
        var suited = TitleCleanupOptions.SuitedTo(RowsInScope().Select(row => row.Title).ToList());
        (FixSeparators, RemoveNumbering, RemoveBracketedText, RemoveNoise, RemoveSharedPrefix, FixCapitals) =
            (suited.FixSeparators, suited.RemoveNumbering, suited.RemoveBracketedText, suited.RemoveNoise, suited.RemoveSharedPrefix, suited.FixCapitals);
        (Find, Replace) = (string.Empty, string.Empty);
        IsOpen = true;
        RefreshPreview();
    }

    [RelayCommand]
    private void Close() => IsOpen = false;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        foreach (var change in Preview)
        {
            change.Row.Title = change.After;
        }

        IsOpen = false;
    }

    private bool CanApply() => ChangeCount > 0;

    private void RefreshPreview()
    {
        var rows = RowsInScope();
        var cleaned = TitleCleaner.CleanAll(rows.Select(row => row.Title).ToList(), BuildOptions());
        Preview.Clear();
        for (var i = 0; i < rows.Count; i++)
        {
            if (!string.Equals(rows[i].Title.Trim(), cleaned[i], StringComparison.Ordinal))
            {
                Preview.Add(new TitleChange(rows[i], rows[i].Title, cleaned[i]));
            }
        }

        ChangeCount = Preview.Count;
        ScopeText = _selection.HasMany ? $"{rows.Count} selected tracks" : $"all {rows.Count} tracks";
    }

    // Selected tracks in playlist order (shared prefixes and numbering are judged across the scope).
    private List<ChapterRowViewModel> RowsInScope() =>
        _selection.HasMany ? _chapters.Where(_selection.Rows.Contains).ToList() : [.. _chapters];

    private TitleCleanupOptions BuildOptions() => new()
    {
        FixSeparators = FixSeparators,
        RemoveNumbering = RemoveNumbering,
        RemoveBracketedText = RemoveBracketedText,
        RemoveNoise = RemoveNoise,
        RemoveSharedPrefix = RemoveSharedPrefix,
        FixCapitals = FixCapitals,
        Find = Find,
        Replace = Replace,
    };
}

/// <summary>One line of the clean-up preview.</summary>
public sealed record TitleChange(ChapterRowViewModel Row, string Before, string After);
