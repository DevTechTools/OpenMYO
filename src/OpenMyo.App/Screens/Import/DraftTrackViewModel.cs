using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Controls;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.App.Services;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Import;

namespace OpenMyo.App.Screens.Import;

/// <summary>A local file waiting to be uploaded, with its editable title and icon.</summary>
public sealed partial class DraftTrackViewModel : ObservableObject, IIconTarget
{
    private readonly TrackDraft _source;
    private readonly IReorderableOwner<DraftTrackViewModel> _owner;
    private readonly IconBitmapLoader _bitmapLoader;
    private readonly DraftTrackActions _actions;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconSourceLabel))]
    private string? _iconMediaId;

    [ObservableProperty]
    private Bitmap? _icon;

    /// <summary>True while the player bar is on this file (playing or paused).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayLabel))]
    private bool _isPlaying;

    public DraftTrackViewModel(
        TrackDraft source, IReorderableOwner<DraftTrackViewModel> owner, IconBitmapLoader bitmapLoader, DraftTrackActions actions)
    {
        (_source, _owner, _bitmapLoader, _actions) = (source, owner, bitmapLoader, actions);
        _title = source.Title;
        _iconMediaId = source.IconMediaId;
        _ = LoadIconAsync();
    }

    public string FileName => Path.GetFileName(_source.SourcePath);

    /// <summary>The file's full path; also how the player identifies this row.</summary>
    public string SourcePath => _source.SourcePath;

    public string PlayLabel => IsPlaying ? "Pause or resume" : "Preview this file";

    public TimeSpan Duration => _source.Duration;

    /// <summary>True when the icon came from the files themselves and no library icon replaced it.</summary>
    public bool UsesLocalArtwork => IconMediaId is null && _source.IconArtwork is not null;

    public bool HasIcon => IconMediaId is not null || _source.IconArtwork is not null;

    public string IconSourceLabel => UsesLocalArtwork ? "From your files" : HasIcon ? "Yoto icon" : "No icon";

    public void AssignIcon(DisplayIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        IconMediaId = icon.MediaId;
    }

    /// <summary>Applies an automatic suggestion unless the files already provide an icon.</summary>
    public void SuggestIcon(DisplayIcon? icon)
    {
        if (icon is not null && !HasIcon)
        {
            IconMediaId = icon.MediaId;
        }
    }

    public TrackDraft ToDraft() => _source with { Title = Title.Trim(), IconMediaId = IconMediaId };

    partial void OnIconMediaIdChanged(string? value)
    {
        OnPropertyChanged(nameof(HasIcon));
        OnPropertyChanged(nameof(UsesLocalArtwork));
        _ = LoadIconAsync();
    }

    private async Task LoadIconAsync() =>
        Icon = UsesLocalArtwork
            ? await _bitmapLoader.LoadArtworkAsync(_source.IconArtwork)
            : await _bitmapLoader.LoadIconAsync(IconMediaId);

    [ObservableProperty]
    private bool _isPicking;

    [RelayCommand]
    private void PickIcon() => _actions.PickIcon(this);

    [RelayCommand]
    private void Play() => _actions.Play(this);

    [RelayCommand]
    private void MoveUp() => _owner.Move(this, -1);

    [RelayCommand]
    private void MoveDown() => _owner.Move(this, 1);

    [RelayCommand]
    private void Remove() => _owner.Remove(this);
}
