using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Controls;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.App.Services;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Editor;

/// <summary>One editable chapter (track row) of an existing playlist.</summary>
public sealed partial class ChapterRowViewModel : ObservableObject, IIconTarget
{
    private readonly Chapter _source;
    private readonly IReorderableOwner<ChapterRowViewModel> _owner;
    private readonly IconBitmapLoader _bitmapLoader;
    private readonly Action<ChapterRowViewModel> _play;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string? _iconMediaId;

    [ObservableProperty]
    private Bitmap? _icon;

    /// <summary>True while the player bar is on this row's track (playing or paused).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayLabel))]
    private bool _isPlaying;

    public ChapterRowViewModel(
        Chapter source, IReorderableOwner<ChapterRowViewModel> owner, IconBitmapLoader bitmapLoader, Action<ChapterRowViewModel> play)
    {
        _source = source;
        _owner = owner;
        _bitmapLoader = bitmapLoader;
        _play = play;
        _title = source.Title;
        _iconMediaId = source.IconMediaId;
        _ = LoadIconAsync();
    }

    /// <summary>Raised when the user edits the row, so the editor can mark itself dirty.</summary>
    public event EventHandler? Changed;

    public TimeSpan Duration => _source.Duration;

    /// <summary>The chapter key, which stays the same when the row is renamed or moved.</summary>
    public string Key => _source.Key;

    public string PlayLabel => IsPlaying ? "Pause or resume" : "Play from this track";

    public bool HasIcon => !string.IsNullOrEmpty(IconMediaId);

    public void AssignIcon(DisplayIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        IconMediaId = icon.MediaId;
    }

    /// <summary>Builds the updated chapter, keeping every track property the editor does not touch.</summary>
    public Chapter ToChapter() => IconAutoAssigner.WithIcon(_source.WithTitle(Title), IconMediaId);

    partial void OnTitleChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);

    partial void OnIconMediaIdChanged(string? value)
    {
        OnPropertyChanged(nameof(HasIcon));
        Changed?.Invoke(this, EventArgs.Empty);
        _ = LoadIconAsync();
    }

    private async Task LoadIconAsync() => Icon = await _bitmapLoader.LoadIconAsync(IconMediaId);

    [RelayCommand]
    private void Play() => _play(this);

    [RelayCommand]
    private void MoveUp() => _owner.Move(this, -1);

    [RelayCommand]
    private void MoveDown() => _owner.Move(this, 1);

    [RelayCommand]
    private void Remove() => _owner.Remove(this);
}
