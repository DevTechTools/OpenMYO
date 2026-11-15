using CommunityToolkit.Mvvm.ComponentModel;
using OpenMyo.Core.Library;

namespace OpenMyo.App.Screens.Library;

/// <summary>
/// A filter chip above the playlist list. <see cref="Issue"/> is <see cref="PlaylistIssues.None"/> for "All";
/// the others toggle and combine.
/// </summary>
public sealed partial class FilterChipViewModel(string title, PlaylistIssues issue) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private int _count;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _isActive;

    public string Title { get; } = title;

    public PlaylistIssues Issue { get; } = issue;

    /// <summary>Problem chips with nothing to fix fade out, so a tidy library reads as done at a glance.</summary>
    public bool IsEmpty => Issue != PlaylistIssues.None && Count == 0 && !IsActive;

    /// <summary>Problem chips use the attention colour for their count, like an unread badge.</summary>
    public bool NeedsAttention => Issue != PlaylistIssues.None && Count > 0;

    partial void OnCountChanged(int value) => OnPropertyChanged(nameof(NeedsAttention));

    /// <summary>How many of <paramref name="rows"/> this chip matches on its own.</summary>
    public void CountIn(IReadOnlyCollection<PlaylistRowViewModel> rows) =>
        Count = rows.Count(row => LibraryFilter.Matches(Issue, row.Issues));
}
