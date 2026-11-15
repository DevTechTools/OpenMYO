using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenMyo.Core.Feeds;

namespace OpenMyo.App.Screens.Import.Feed;

/// <summary>One episode in the feed preview, ticked when it should be downloaded.</summary>
public sealed partial class FeedEpisodeRowViewModel(FeedEpisode episode, Action selectionChanged) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>0 to 1 while this episode downloads, so several moving at once are visible.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDownloadStarted))]
    private double _downloadProgress;

    public bool HasDownloadStarted => DownloadProgress > 0;

    public FeedEpisode Episode { get; } = episode;

    public string Title => Episode.Title;

    /// <summary>"12 Mar 2025 · 24 min"; parts the feed doesn't give are left out.</summary>
    public string Details => string.Join(" · ", new[]
    {
        Episode.Published?.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture),
        Episode.Duration is { } duration ? $"{Math.Max(1, Math.Round(duration.TotalMinutes))} min" : null,
    }.OfType<string>());

    partial void OnIsSelectedChanged(bool value) => selectionChanged();
}
