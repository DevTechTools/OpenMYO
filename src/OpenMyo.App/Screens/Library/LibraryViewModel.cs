using System.Collections.ObjectModel;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.Editor;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Library;
using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Library;

/// <summary>
/// The playlist library: lists every card immediately, then loads details in the background so the
/// filter chips (no tracks, missing icons, missing cover) fill in progressively.
/// </summary>
public sealed partial class LibraryViewModel : OperationViewModel
{
    private const int DetailLoadParallelism = 6;

    private readonly IPlaylistRepository _repository;
    private readonly IconBitmapLoader _bitmapLoader;
    private readonly ISettingsStore _settings;
    private List<PlaylistRowViewModel> _all = [];

    private PlaylistIssues _requiredIssues;
    private SearchQuery _query = SearchQuery.Parse(null);

    /// <summary>The track to select once the playlist opens, after a click on a matching track.</summary>
    private string? _pendingReveal;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedCount;

    /// <summary>"4 playlists · 9 tracks" under the search field; null when not searching.</summary>
    [ObservableProperty]
    private string? _searchSummary;

    [ObservableProperty]
    private bool _isGridView;

    public LibraryViewModel(
        IPlaylistRepository repository,
        IconBitmapLoader bitmapLoader,
        ISettingsStore settings,
        PlaylistEditorViewModel editor,
        BulkActionsViewModel bulk,
        ILogger<LibraryViewModel> logger)
        : base(logger)
    {
        (_repository, _bitmapLoader, _settings) = (repository, bitmapLoader, settings);
        Editor = editor;
        Bulk = bulk;
        Bulk.RowsUpdated += (_, _) => ApplyFilter();
        Bulk.RowsDeleted += (_, rows) => RemoveRows(rows);
        Selection.SelectionChanged += (_, _) => OnSelectionChanged();
        Editor.PlaylistCreated += (_, playlist) => AddCreated(playlist);
    }

    /// <summary>Raised once every playlist's details have loaded.</summary>
    public event EventHandler<IReadOnlyList<Playlist>>? PlaylistsLoaded;

    public PlaylistEditorViewModel Editor { get; }

    public BulkActionsViewModel Bulk { get; }

    public ObservableCollection<PlaylistRowViewModel> Visible { get; } = [];

    /// <summary>Filter chips: "All", then one per problem; problem chips combine.</summary>
    public IReadOnlyList<FilterChipViewModel> Chips { get; } =
    [
        new("All", PlaylistIssues.None) { IsActive = true },
        new("No tracks", PlaylistIssues.NoTracks),
        new("Missing icons", PlaylistIssues.MissingIcons),
        new("Missing cover", PlaylistIssues.MissingCover),
    ];

    public SelectionModel<PlaylistRowViewModel> Selection { get; } = new() { SingleSelect = false };

    public IReadOnlyList<PlaylistRowViewModel> SelectedRows => Selection.SelectedItems.OfType<PlaylistRowViewModel>().ToList();

    public bool HasBulkSelection => SelectedCount > 1;

    /// <summary>Restores the last list/grid choice.</summary>
    public async Task LoadLayoutAsync(CancellationToken cancellationToken) =>
        IsGridView = (await _settings.LoadAsync(cancellationToken)).IsLibraryGrid;

    [RelayCommand]
    private async Task ToggleLayoutAsync()
    {
        IsGridView = !IsGridView;
        var current = await _settings.LoadAsync(CancellationToken.None);
        await _settings.SaveAsync(current with { IsLibraryGrid = IsGridView }, CancellationToken.None);
    }

    [RelayCommand]
    public Task RefreshAsync() => RunAsync("Loading playlists", async token =>
    {
        var summaries = await _repository.ListAsync(token);
        _all = summaries.Select(summary => new PlaylistRowViewModel(summary, _bitmapLoader)).ToList();
        ApplyFilter();
        await LoadDetailsAsync(_all, token);
        ApplyFilter();
        PlaylistsLoaded?.Invoke(this, _all.Select(row => row.Playlist).OfType<Playlist>().ToList());
    });

    /// <summary>Opens a blank, unsaved playlist in the editor.</summary>
    [RelayCommand]
    private void NewPlaylist()
    {
        Selection.Clear();
        Editor.OpenNew();
    }

    /// <summary>"All" clears the filter; a problem chip toggles, so several can narrow the list together.</summary>
    [RelayCommand]
    private void ToggleFilter(FilterChipViewModel chip)
    {
        SetRequiredIssues(chip.Issue == PlaylistIssues.None ? PlaylistIssues.None : _requiredIssues ^ chip.Issue);
        ApplyFilter();
    }

    private void SetRequiredIssues(PlaylistIssues required)
    {
        _requiredIssues = required;
        foreach (var chip in Chips)
        {
            chip.IsActive = chip.Issue == PlaylistIssues.None ? required == PlaylistIssues.None : required.HasFlag(chip.Issue);
        }
    }

    private void UpdateCounts()
    {
        foreach (var chip in Chips)
        {
            chip.CountIn(_all);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _query = SearchQuery.Parse(value);
        ApplyFilter();
    }

    /// <summary>Opens the playlist a matching track belongs to, with that track selected and scrolled into view.</summary>
    [RelayCommand]
    private void OpenTrackMatch(TrackMatchViewModel match)
    {
        if (SelectedRows is [var only] && only == match.Row)
        {
            Editor.Selection.SelectKey(match.ChapterKey);
            return;
        }

        _pendingReveal = match.ChapterKey;
        Selection.Clear();
        Selection.Select(Visible.IndexOf(match.Row));
    }

    partial void OnSelectedCountChanged(int value) => OnPropertyChanged(nameof(HasBulkSelection));

    /// <summary>Re-evaluates counts and the visible list after rows changed (e.g. bulk edits).</summary>
    private void ApplyFilter()
    {
        UpdateCounts();
        var selected = SelectedRows.ToHashSet();
        var visible = _all
            .Where(row => row.ApplySearch(_query) && LibraryFilter.Matches(_requiredIssues, row.Issues))
            .OrderBy(SearchRank)
            .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Visible.Clear();
        foreach (var row in visible)
        {
            Visible.Add(row);
        }

        RestoreSelection(selected);
        UpdateSearchSummary();
    }

    /// <summary>Playlists whose title matches come before those that match only on a track.</summary>
    private static int SearchRank(PlaylistRowViewModel row) => row.Match is { IsTitleMatch: false } ? 1 : 0;

    /// <summary>
    /// Shows a row that matches the search only now its tracks have loaded. It is inserted in place rather than
    /// rebuilding the list, so the open playlist and its unsaved edits stay as they are.
    /// </summary>
    private void ShowIfNewlyMatching(PlaylistRowViewModel row)
    {
        var matches = row.ApplySearch(_query) && LibraryFilter.Matches(_requiredIssues, row.Issues);
        if (matches && !Visible.Contains(row))
        {
            var index = Visible.TakeWhile(other => SearchRank(other) < SearchRank(row)
                || (SearchRank(other) == SearchRank(row)
                    && StringComparer.CurrentCultureIgnoreCase.Compare(other.Title, row.Title) <= 0)).Count();
            Visible.Insert(index, row);
        }

        UpdateSearchSummary();
    }

    private void UpdateSearchSummary()
    {
        if (_query.IsEmpty)
        {
            SearchSummary = null;
            return;
        }

        var tracks = Visible.Sum(row => row.Match?.Tracks.Count ?? 0);
        var pending = _all.Count(row => !row.IsLoaded);
        var found = Visible.Count == 0
            ? $"No playlists or tracks match \u201c{SearchText.Trim()}\u201d"
            : $"{Plural(Visible.Count, "playlist")} · {Plural(tracks, "track")}";
        SearchSummary = pending == 0 ? found : $"{found} · still checking {Plural(pending, "playlist")}";
    }

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>Adds a playlist the editor just created and selects it, clearing filters that would hide it.</summary>
    private void AddCreated(Playlist playlist)
    {
        var summary = new PlaylistSummary(playlist.CardId!, playlist.Title, playlist.UpdatedAt, playlist.CoverImageUrl);
        var row = new PlaylistRowViewModel(summary, _bitmapLoader);
        row.UpdateFrom(playlist);
        _all.Add(row);
        SearchText = string.Empty;
        SetRequiredIssues(PlaylistIssues.None);
        ApplyFilter();
        Selection.Clear();
        Selection.Select(Visible.IndexOf(row));
    }

    private void RemoveRows(IReadOnlyCollection<PlaylistRowViewModel> rows)
    {
        _all = _all.Except(rows).ToList();
        ApplyFilter();
    }

    private async Task LoadDetailsAsync(List<PlaylistRowViewModel> rows, CancellationToken token)
    {
        var loaded = 0;

        // Progress reports arrive on the UI thread, which is where rows and counts may change.
        IProgress<(PlaylistRowViewModel Row, Playlist Playlist)> applied = new Progress<(PlaylistRowViewModel Row, Playlist Playlist)>(result =>
        {
            result.Row.UpdateFrom(result.Playlist);
            UpdateCounts();
            if (!_query.IsEmpty)
            {
                ShowIfNewlyMatching(result.Row);
            }

            ReportProgress((double)++loaded / rows.Count, $"Checking playlists {loaded} of {rows.Count}");
        });

        var options = new ParallelOptions { MaxDegreeOfParallelism = DetailLoadParallelism, CancellationToken = token };
        await Parallel.ForEachAsync(rows, options, async (row, rowToken) =>
            applied.Report((row, await _repository.GetAsync(row.Summary, rowToken))));
    }

    private void RestoreSelection(HashSet<PlaylistRowViewModel> selected)
    {
        Selection.Source = Visible;
        for (var i = 0; i < Visible.Count; i++)
        {
            if (selected.Contains(Visible[i]))
            {
                Selection.Select(i);
            }
        }
    }

    private void OnSelectionChanged()
    {
        SelectedCount = Selection.SelectedItems.Count;
        OnPropertyChanged(nameof(SelectedRows));
        Bulk.SelectedRows = SelectedRows;
        var row = SelectedCount == 1 ? SelectedRows[0] : null;

        // Opened from search: show the matching track. Clear() fires first with no row, so keep the key for the next call.
        var reveal = row is null ? null : _pendingReveal ?? row.FirstMatchKey;
        if (row is not null)
        {
            _pendingReveal = null;
        }

        OpenAndRevealAsync(row, reveal).Forget(Logger, "opening a playlist");
    }

    private async Task OpenAndRevealAsync(PlaylistRowViewModel? row, string? chapterKey)
    {
        await Editor.OpenAsync(row);
        if (chapterKey is not null)
        {
            Editor.Selection.SelectKey(chapterKey);
        }
    }
}
