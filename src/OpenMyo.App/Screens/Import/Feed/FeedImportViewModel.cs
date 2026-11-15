using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Services;
using OpenMyo.Core.Feeds;
using OpenMyo.Core.Import;

namespace OpenMyo.App.Screens.Import.Feed;

/// <summary>
/// The "From Feed" dialog: the user enters a feed address, previews its episodes (the latest
/// <see cref="FeedImporter.MaxEpisodes"/> ticked), narrows them with length and title filters, picks the track
/// order, then downloads the ticked ones into a playlist draft.
/// </summary>
public sealed partial class FeedImportViewModel(FeedImporter importer, IconBitmapLoader bitmapLoader, ILogger<FeedImportViewModel> logger)
    : OperationViewModel(logger)
{
    // Every episode in the feed, oldest first; Episodes is the filtered, ordered view of these.
    private List<FeedEpisodeRowViewModel> _rows = [];
    private bool _isBulkSelecting;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFeed))]
    private PodcastFeed? _feed;

    [ObservableProperty]
    private Bitmap? _cover;

    [ObservableProperty]
    private IReadOnlyList<FeedEpisodeRowViewModel> _episodes = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionSummary), nameof(IsOverLimit))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private int _selectedCount;

    [ObservableProperty]
    private decimal? _minMinutes;

    [ObservableProperty]
    private decimal? _maxMinutes;

    [ObservableProperty]
    private string _excludeText = string.Empty;

    [ObservableProperty]
    private string _includeText = string.Empty;

    /// <summary>Sets both the preview order and the track order on the card.</summary>
    [ObservableProperty]
    private bool _newestFirst;

    /// <summary>Raised with the downloaded draft, ready for review on the import screen.</summary>
    public event EventHandler<PlaylistDraft>? DraftReady;

    public bool HasFeed => Feed is not null;

    public bool IsOverLimit => SelectedCount > FeedImporter.MaxEpisodes;

    public string SelectionSummary
    {
        get
        {
            var hidden = _rows.Count - Episodes.Count;
            var hiddenNote = hidden > 0 ? $" · {hidden} hidden by filters" : string.Empty;
            return IsOverLimit
                ? $"{SelectedCount} selected. A playlist holds up to {FeedImporter.MaxEpisodes} tracks."
                : $"{SelectedCount} of {Episodes.Count} selected{hiddenNote}";
        }
    }

    /// <summary>Opens the dialog, loading <paramref name="url"/> straight away when one was pasted.</summary>
    public void Open(string? url = null)
    {
        IsOpen = true;
        if (url is not null && FeedUrl.TryParse(url) is not null && !IsBusy)
        {
            Url = url.Trim();
            LoadCommand.Execute(null);
        }
    }

    /// <summary>Deletes the downloads behind a draft that was published or dropped.</summary>
    public void Discard(PlaylistDraft draft) => importer.Discard(draft);

    partial void OnMinMinutesChanged(decimal? value) => ApplyFilter();

    partial void OnMaxMinutesChanged(decimal? value) => ApplyFilter();

    partial void OnExcludeTextChanged(string value) => ApplyFilter();

    partial void OnIncludeTextChanged(string value) => ApplyFilter();

    partial void OnNewestFirstChanged(bool value) => ApplyFilter();

    [RelayCommand]
    private void Close()
    {
        CancelCommand.Execute(null);
        (IsOpen, Feed, Cover, Error, Status, Url) = (false, null, null, null, null, string.Empty);
        (MinMinutes, MaxMinutes, ExcludeText, IncludeText, NewestFirst) = (null, null, string.Empty, string.Empty, false);
        _rows = [];
        Episodes = [];
        SelectedCount = 0;
    }

    private bool CanLoad() => FeedUrl.TryParse(Url) is not null;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private Task LoadAsync() => RunAsync("Reading feed", async token =>
    {
        var feedUrl = FeedUrl.TryParse(Url) ?? throw new FeedFormatException("Enter a feed address.");
        var feed = await importer.LoadAsync(feedUrl, token);
        _rows = feed.Episodes.Select(episode => new FeedEpisodeRowViewModel(episode, OnRowSelectionChanged)).ToList();
        Feed = feed;
        ApplyFilter();
        SelectLatest();
        Cover = await bitmapLoader.LoadCoverAsync(feed.ImageUrl);
    });

    /// <summary>Ticks the newest <see cref="FeedImporter.MaxEpisodes"/> episodes the filters let through.</summary>
    [RelayCommand]
    private void SelectLatest()
    {
        var newestFirst = NewestFirst ? Episodes : Episodes.Reverse();
        var latest = newestFirst.Take(FeedImporter.MaxEpisodes).ToHashSet();
        SetSelection(row => latest.Contains(row));
    }

    /// <summary>Ticks every episode the filters let through, even past the track limit (the summary then says so).</summary>
    [RelayCommand]
    private void SelectAll() => SetSelection(_ => true);

    [RelayCommand]
    private void SelectNone() => SetSelection(_ => false);

    private bool CanDownload() => SelectedCount is > 0 and <= FeedImporter.MaxEpisodes;

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private Task DownloadAsync() => RunAsync("Downloading episodes", async token =>
    {
        if (Feed is not { } feed)
        {
            return;
        }

        var selected = Episodes.Where(row => row.IsSelected).Select(row => row.Episode).ToHashSet();
        var order = NewestFirst ? EpisodeOrder.NewestFirst : EpisodeOrder.OldestFirst;
        var rows = Episodes.ToDictionary(row => row.Episode);
        foreach (var row in Episodes)
        {
            row.DownloadProgress = 0; // a cancelled or failed earlier attempt left its bars behind
        }

        var progress = new Progress<FeedDownloadProgress>(p =>
        {
            ReportProgress(p.Fraction, p.Status);
            if (p.Episode is not null && rows.TryGetValue(p.Episode, out var row))
            {
                row.DownloadProgress = p.EpisodeFraction;
            }
        });
        var draft = await importer.DownloadAsync(feed, selected, order, progress, token);
        Close();
        DraftReady?.Invoke(this, draft);
    });

    /// <summary>Hidden episodes are unticked, so what is downloaded is always what is shown.</summary>
    private void ApplyFilter()
    {
        var filter = new EpisodeFilter(
            Minutes(MinMinutes), Minutes(MaxMinutes), EpisodeFilter.ParseWords(ExcludeText), EpisodeFilter.ParseWords(IncludeText));
        var visible = _rows.Where(row => filter.Matches(row.Episode)).ToList();
        var shown = visible.ToHashSet();
        Episodes = NewestFirst ? Enumerable.Reverse(visible).ToList() : visible;
        SetSelection(row => row.IsSelected && shown.Contains(row), _rows);
    }

    private void SetSelection(Func<FeedEpisodeRowViewModel, bool> isSelected) => SetSelection(isSelected, Episodes);

    // Counting once at the end keeps ticking thousands of rows from recounting after each one.
    private void SetSelection(Func<FeedEpisodeRowViewModel, bool> isSelected, IReadOnlyList<FeedEpisodeRowViewModel> rows)
    {
        _isBulkSelecting = true;
        foreach (var row in rows)
        {
            row.IsSelected = isSelected(row);
        }

        _isBulkSelecting = false;
        OnRowSelectionChanged();
    }

    private void OnRowSelectionChanged()
    {
        if (_isBulkSelecting)
        {
            return;
        }

        SelectedCount = Episodes.Count(row => row.IsSelected);
        OnPropertyChanged(nameof(SelectionSummary));
    }

    private static TimeSpan? Minutes(decimal? minutes) => minutes is > 0 ? TimeSpan.FromMinutes((double)minutes.Value) : null;
}
