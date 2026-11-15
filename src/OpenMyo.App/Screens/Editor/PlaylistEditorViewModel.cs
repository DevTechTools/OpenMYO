using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Controls;
using OpenMyo.App.Screens.Editor.Commands;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.App.Screens.Library;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playback;
using OpenMyo.Core.Playlists;
using OpenMyo.Core.Publishing;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.Editor;

/// <summary>Edits one playlist (existing, or a new unsaved draft): titles, order, icons, cover and added tracks.</summary>
public sealed partial class PlaylistEditorViewModel
    : OperationViewModel, IReorderableOwner<ChapterRowViewModel>, IReorderableList, IChapterEditor
{
    private readonly IPlaylistRepository _repository;
    private readonly IconBitmapLoader _bitmapLoader;
    private readonly IFilePickerService _filePicker;
    private readonly PlaylistMediaService _mediaService;
    private Playlist? _original;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlaylist))]
    private PlaylistRowViewModel? _row;

    /// <summary>True while showing a new playlist that does not exist on Yoto yet.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlaylist))]
    private bool _isDraft;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private Bitmap? _cover;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(RevertCommand))]
    private bool _isDirty;

    /// <summary>True while the icon panel is open; it opens from a row's icon and closes with ✕ or Escape.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPicker))]
    private bool _isPickingIcon;

    public PlaylistEditorViewModel(
        IPlaylistRepository repository,
        EditorIconServices iconServices,
        AiTitleRenamer titleRenamer,
        IconBitmapLoader bitmapLoader,
        IconPickerViewModel picker,
        IFilePickerService filePicker,
        PlaylistMediaService mediaService,
        ILogger<PlaylistEditorViewModel> logger)
        : base(logger)
    {
        (_repository, _bitmapLoader, _filePicker, _mediaService) = (repository, bitmapLoader, filePicker, mediaService);
        Picker = picker;
        Selection = new ChapterSelection(Chapters);
        Suggestions = new TrackSuggestionsViewModel(Chapters);
        Batch = new ChapterBatchCommands(Chapters, Selection, () => IsDirty = true);
        Cleanup = new TitleCleanupViewModel(Chapters, Selection);
        Icons = new ChapterIconCommands(iconServices, picker, Selection, Suggestions, this);
        Titles = new ChapterTitleCommands(titleRenamer, iconServices, picker, Suggestions, this);

        // While open, the picker follows the selected tracks; choosing an icon moves on to the next track without one.
        Selection.Changed += (_, _) => OpenPickerForSelection();
        Picker.IconChosen += (_, _) => Selection.SelectNextWithoutIcon();
        Picker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IconPickerViewModel.IsAskingForKey))
            {
                OnPropertyChanged(nameof(ShowPicker));
            }
        };
    }

    /// <summary>Raised when a draft is first saved to Yoto, with the created playlist.</summary>
    public event EventHandler<Playlist>? PlaylistCreated;

    /// <summary>Raised when the user presses play on a row, with the playlist in its on-screen order.</summary>
    public event EventHandler<PlaylistPlayRequest>? PlayRequested;

    Playlist? IChapterEditor.CurrentPlaylist => _original is null ? null : BuildPlaylist();

    public ChapterIconCommands Icons { get; }

    public ChapterTitleCommands Titles { get; }

    public TitleCleanupViewModel Cleanup { get; }

    public ChapterSelection Selection { get; }

    public TrackSuggestionsViewModel Suggestions { get; }

    public ChapterBatchCommands Batch { get; }

    public IconPickerViewModel Picker { get; }

    public ObservableCollection<ChapterRowViewModel> Chapters { get; } = [];

    public bool HasPlaylist => Row is not null || IsDraft;

    /// <summary>The icon panel shows only while picking, or while it asks for an API key an AI action needs.</summary>
    public bool ShowPicker => IsPickingIcon || Picker.IsAskingForKey;

    public async Task OpenAsync(PlaylistRowViewModel? row)
    {
        Row = row;
        ShowPlaylist(row?.Playlist);
        if (row is { IsLoaded: false })
        {
            await RunAsync("Loading playlist", async token => ShowPlaylist(await LoadAsync(row, token)));
        }
    }

    /// <summary>Shows a blank draft; nothing is created on Yoto until it is saved or media is added.</summary>
    public void OpenNew()
    {
        Row = null;
        ShowPlaylist(new Playlist { Title = "Untitled playlist" });
    }

    /// <summary>Opens the icon panel for <paramref name="row"/>, or for the whole selection when the row is part of it.</summary>
    [RelayCommand]
    private void PickIcon(ChapterRowViewModel row)
    {
        IsPickingIcon = true;
        if (!Selection.Rows.Contains(row))
        {
            Selection.Select(row);
        }

        OpenPickerForSelection();
    }

    [RelayCommand]
    private void ClosePicker()
    {
        IsPickingIcon = false;
        Picker.IsAskingForKey = false;
        Picker.Open(null);
    }

    private void OpenPickerForSelection()
    {
        if (IsPickingIcon)
        {
            Picker.Open(Selection.Target, Selection.TrackNumber);
        }
    }

    /// <summary>Marks the row the player is on; <paramref name="rowKey"/> is null when nothing plays.</summary>
    public void ShowPlaying(string? rowKey)
    {
        foreach (var row in Chapters)
        {
            row.IsPlaying = string.Equals(PlaybackLists.RowKey(_original?.CardId, row.Key), rowKey, StringComparison.Ordinal);
        }
    }

    public void Move(ChapterRowViewModel row, int offset)
    {
        var from = Chapters.IndexOf(row);
        MoveItem(from, from + offset);
    }

    public void MoveItem(int fromIndex, int toIndex)
    {
        var to = Math.Clamp(toIndex, 0, Chapters.Count - 1);
        if (fromIndex >= 0 && fromIndex != to)
        {
            Chapters.Move(fromIndex, to);
            IsDirty = true;
        }
    }

    public void Remove(ChapterRowViewModel row)
    {
        IsDirty |= Chapters.Remove(row);
    }

    Task IChapterEditor.RunAsync(string status, Func<CancellationToken, Task> operation) => RunAsync(status, operation);

    void IChapterEditor.ApplyChapters(IReadOnlyList<Chapter> chapters)
    {
        ShowChapters(chapters);
        IsDirty = true;
    }

    partial void OnTitleChanged(string value) => IsDirty = _original is not null && (IsDraft || value != _original.Title);

    [RelayCommand(CanExecute = nameof(IsDirty))]
    private Task SaveAsync() => RunAsync("Saving", async token => await SaveAndShowAsync(BuildPlaylist(), token));

    [RelayCommand(CanExecute = nameof(IsDirty))]
    private void Revert() => ShowPlaylist(_original);

    [RelayCommand]
    private async Task AddTracksAsync()
    {
        var files = await _filePicker.PickAudioFilesAsync();
        if (files.Count == 0 || _original is null)
        {
            return;
        }

        var progress = new Progress<PublishProgress>(p => ReportProgress(p.Fraction, p.Status));
        await RunAsync("Uploading", async token =>
            await SaveAndShowAsync(await _mediaService.AppendFilesAsync(BuildPlaylist(), files, progress, token), token, alreadySaved: true));
    }

    [RelayCommand]
    private async Task ChangeCoverAsync()
    {
        var image = await _filePicker.PickImageAsync();
        if (image is not null && _original is not null)
        {
            await RunAsync("Uploading cover", async token =>
                await SaveAndShowAsync(await _mediaService.ReplaceCoverAsync(BuildPlaylist(), image, token), token));
        }
    }

    private async Task SaveAndShowAsync(Playlist playlist, CancellationToken token, bool alreadySaved = false)
    {
        var saved = alreadySaved ? playlist : await _repository.SaveAsync(playlist, token);
        var created = IsDraft;
        Row?.UpdateFrom(saved);
        ShowPlaylist(saved);
        if (created)
        {
            PlaylistCreated?.Invoke(this, saved);
        }
    }

    private async Task<Playlist> LoadAsync(PlaylistRowViewModel row, CancellationToken token)
    {
        var playlist = await _repository.GetAsync(row.Summary, token);
        row.UpdateFrom(playlist);
        return playlist;
    }

    private Playlist BuildPlaylist() =>
        (_original ?? throw new InvalidOperationException("No playlist is open.")) with
        {
            Title = Title.Trim(),
            Chapters = Chapters.Select(chapter => chapter.ToChapter()).ToList(),
        };

    private void ShowPlaylist(Playlist? playlist)
    {
        if (playlist is null || !string.Equals(playlist.CardId, _original?.CardId, StringComparison.Ordinal))
        {
            Suggestions.Reset();
        }

        _original = playlist;
        IsDraft = playlist is { CardId: null };
        Cleanup.CloseCommand.Execute(null);
        Title = playlist?.Title ?? string.Empty;
        ShowChapters(playlist?.Chapters ?? []);
        IsDirty = IsDraft;
        _ = LoadCoverAsync(playlist?.CoverImageUrl);
    }

    private void ShowChapters(IReadOnlyList<Chapter> chapters)
    {
        Chapters.Clear();
        foreach (var chapter in chapters)
        {
            var row = new ChapterRowViewModel(chapter, this, _bitmapLoader, RequestPlay);
            row.Changed += OnRowChanged;
            Chapters.Add(row);
        }

        Suggestions.Refresh();

        // Picking carries over to the next playlist, ready on its first track without an icon.
        if (IsPickingIcon)
        {
            Selection.SelectFirstWithoutIcon();
        }
    }

    // Only saved playlists have audio on Yoto; a draft gets its first tracks through Add Tracks, which saves it.
    private void RequestPlay(ChapterRowViewModel row)
    {
        if (_original is { CardId: not null })
        {
            PlayRequested?.Invoke(this, new PlaylistPlayRequest(BuildPlaylist(), row.Key));
        }
    }

    private void OnRowChanged(object? sender, EventArgs e)
    {
        IsDirty = true;
        Suggestions.Refresh();
    }

    private async Task LoadCoverAsync(Uri? url) => Cover = await _bitmapLoader.LoadCoverAsync(url);
}
