using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.App.Screens.Import.Feed;
using OpenMyo.App.Services;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Import;
using OpenMyo.Core.Publishing;

namespace OpenMyo.App.Screens.Import;

/// <summary>
/// Bulk import: drop or choose folders/files (or download a podcast feed), review the cleaned-up drafts (titles, order, icons),
/// then create every playlist with one click.
/// </summary>
public sealed partial class ImportViewModel : OperationViewModel
{
    private readonly DraftBuilder _draftBuilder;
    private readonly IIconSuggester _iconSuggester;
    private readonly PlaylistPublisher _publisher;
    private readonly IconBitmapLoader _bitmapLoader;
    private readonly IFilePickerService _filePicker;

    [ObservableProperty]
    private DraftTrackViewModel? _pickingTrack;

    public ImportViewModel(
        DraftBuilder draftBuilder,
        IIconSuggester iconSuggester,
        PlaylistPublisher publisher,
        IconBitmapLoader bitmapLoader,
        IFilePickerService filePicker,
        IconPickerViewModel picker,
        FeedImportViewModel feed,
        ILogger<ImportViewModel> logger)
        : base(logger)
    {
        (_draftBuilder, _iconSuggester, _publisher, _bitmapLoader, _filePicker) = (draftBuilder, iconSuggester, publisher, bitmapLoader, filePicker);
        Picker = picker;
        Feed = feed;
        Feed.DraftReady += (_, draft) => AddFeedDraft(draft);
        Picker.IconChosen += (_, _) => PickNextTrackWithoutIcon();
        Drafts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasDrafts));
    }

    /// <summary>Raised after playlists are created so the library can refresh.</summary>
    public event EventHandler? PlaylistsCreated;

    /// <summary>Raised when the user previews a file, with its draft in on-screen order and the file's position.</summary>
    public event EventHandler<DraftPlayRequest>? PlayRequested;

    public IconPickerViewModel Picker { get; }

    /// <summary>The "From Feed" dialog.</summary>
    public FeedImportViewModel Feed { get; }

    public ObservableCollection<DraftCardViewModel> Drafts { get; } = [];

    public bool HasDrafts => Drafts.Count > 0;

    /// <summary>Entry point for drag-and-drop as well as the pickers.</summary>
    public Task AddPathsAsync(IReadOnlyList<string> paths) => RunAsync("Reading files", async token =>
        await AddDraftsAsync(await _draftBuilder.BuildAsync(paths, token), token));

    /// <summary>
    /// The card is added at once so downloaded files are never lost; icon matching runs if the screen is free
    /// (otherwise the user picks icons, as for any track without a match).
    /// </summary>
    private void AddFeedDraft(PlaylistDraft draft)
    {
        var added = AddCards([draft]);
        RunAsync("Finding icons", token => SuggestIconsAsync(added, token)).Forget(Logger, "matching feed icons");
    }

    private async Task AddDraftsAsync(IReadOnlyList<PlaylistDraft> drafts, CancellationToken token) =>
        await SuggestIconsAsync(AddCards(drafts), token);

    private List<DraftCardViewModel> AddCards(IReadOnlyList<PlaylistDraft> drafts)
    {
        var added = drafts.Select(draft => new DraftCardViewModel(draft, _bitmapLoader, RemoveDraft, new DraftTrackActions(PickIcon, RequestPlay))).ToList();
        foreach (var draft in added)
        {
            Drafts.Add(draft);
        }

        return added;
    }

    private async Task SuggestIconsAsync(List<DraftCardViewModel> drafts, CancellationToken token)
    {
        ReportProgress(0, "Finding icons");
        foreach (var draft in drafts)
        {
            await SuggestIconsAsync(draft, token);
        }
    }

    /// <summary>Marks the file the player is on; <paramref name="rowKey"/> is null when nothing plays.</summary>
    public void ShowPlaying(string? rowKey)
    {
        foreach (var track in Drafts.SelectMany(draft => draft.Tracks))
        {
            track.IsPlaying = string.Equals(track.SourcePath, rowKey, StringComparison.Ordinal);
        }
    }

    partial void OnPickingTrackChanged(DraftTrackViewModel? oldValue, DraftTrackViewModel? newValue)
    {
        oldValue?.IsPicking = false;
        newValue?.IsPicking = true;
        Picker.Open(newValue);
    }

    [RelayCommand]
    private async Task ChooseFoldersAsync() => await AddPathsAsync(await _filePicker.PickFoldersAsync());

    [RelayCommand]
    private async Task ChooseFilesAsync() => await AddPathsAsync(await _filePicker.PickAudioFilesAsync());

    [RelayCommand]
    private void ChooseFeed() => Feed.Open();

    [RelayCommand]
    private void Clear()
    {
        foreach (var draft in Drafts)
        {
            Feed.Discard(draft.ToDraft());
        }

        Drafts.Clear();
        PickingTrack = null;
    }

    [RelayCommand]
    private Task CreateAllAsync() => RunAsync("Creating playlists", async token =>
    {
        foreach (var draft in Drafts.ToList())
        {
            var progress = new Progress<PublishProgress>(p => (draft.Progress, draft.Status) = (p.Fraction, p.Status));
            await _publisher.PublishAsync(draft.ToDraft(), progress, token);
            RemoveDraft(draft);
            PlaylistsCreated?.Invoke(this, EventArgs.Empty);
        }
    });

    private async Task SuggestIconsAsync(DraftCardViewModel draft, CancellationToken token)
    {
        // Automatic matching is always the free trigram matcher; AI is only used when asked for in the picker.
        var request = new IconSuggestionRequest(draft.Title, draft.Tracks.Select(track => track.Title).ToList());
        var suggestions = await _iconSuggester.SuggestAsync(request, token);
        for (var i = 0; i < suggestions.Count && i < draft.Tracks.Count; i++)
        {
            draft.Tracks[i].SuggestIcon(suggestions[i].Best);
        }
    }

    private void PickIcon(DraftTrackViewModel track) => PickingTrack = track;

    private void RequestPlay(DraftTrackViewModel track)
    {
        if (Drafts.FirstOrDefault(draft => draft.Tracks.Contains(track)) is { } card)
        {
            PlayRequested?.Invoke(this, new DraftPlayRequest(card.ToDraft(), card.Tracks.IndexOf(track)));
        }
    }

    private void PickNextTrackWithoutIcon()
    {
        var all = Drafts.SelectMany(draft => draft.Tracks).ToList();
        PickingTrack = IconTargetSequence.NextWithoutIcon(all, PickingTrack) ?? PickingTrack;
    }

    private void RemoveDraft(DraftCardViewModel draft)
    {
        Drafts.Remove(draft);
        Feed.Discard(draft.ToDraft());
        if (PickingTrack is not null && draft.Tracks.Contains(PickingTrack))
        {
            PickingTrack = null;
        }
    }
}
