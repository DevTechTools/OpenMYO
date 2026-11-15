using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.Editor;

/// <summary>
/// Fix-ups the editor offers only when they apply ("12 tracks have no icon"), each one click away.
/// </summary>
public sealed partial class TrackSuggestionsViewModel : ObservableObject
{
    private readonly ObservableCollection<ChapterRowViewModel> _chapters;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMissingIcons), nameof(MissingIconsText), nameof(HasAny))]
    private int _missingIconCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTidy), nameof(TidyText), nameof(TidyExample), nameof(ShowOrder), nameof(HasAny))]
    private TitleReview _review = new(0, null, false);

    /// <summary>A one-off note about the last batch action, e.g. numbers Yoto has no icon for.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotice), nameof(HasAny))]
    private string? _notice;

    public TrackSuggestionsViewModel(ObservableCollection<ChapterRowViewModel> chapters)
    {
        _chapters = chapters;
        chapters.CollectionChanged += (_, _) => Refresh();
    }

    public bool ShowMissingIcons => MissingIconCount > 0;

    public string MissingIconsText => MissingIconCount == 1 ? "1 missing icon" : $"{MissingIconCount} missing icons";

    public bool ShowTidy => Review.TidyCount > 0;

    public string TidyText => Review.TidyCount == 1 ? "Tidy 1 title" : $"Tidy {Review.TidyCount} titles";

    /// <summary>Tooltip: one real example of what tidying would do.</summary>
    public string? TidyExample => Review.TidyExample;

    public bool ShowOrder => Review.IsOutOfOrder;

    public bool HasNotice => Notice is not null;

    public bool HasAny => ShowMissingIcons || ShowTidy || ShowOrder;

    public void Refresh()
    {
        MissingIconCount = _chapters.Count(chapter => !chapter.HasIcon);
        Review = TitleReview.Evaluate(_chapters.Select(chapter => chapter.Title).ToList());
    }

    /// <summary>Called when another playlist opens.</summary>
    public void Reset() => Notice = null;

    [RelayCommand]
    private void DismissNotice() => Notice = null;
}
