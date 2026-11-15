using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;
using OpenMyo.Core.Playback;
using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// The now-playing bar along the bottom of the window. It plays a list (a playlist from Yoto or a folder about to be
/// imported) track after track, and keeps playing while the user moves between screens.
/// </summary>
public sealed partial class NowPlayingViewModel : OperationViewModel
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    // Position updates from the timer and from the slider share one property; smaller moves are the timer's own.
    private const double SeekToleranceSeconds = 1.5;

    private readonly IAudioPlayer _player;
    private readonly ITrackStreamSource _streams;
    private readonly DispatcherTimer _timer;
    private PlaybackList? _list;
    private bool _isUpdatingPosition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrent), nameof(ShowBar))]
    private PlaybackItem? _current;

    /// <summary>Where the list came from: the playlist or draft title.</summary>
    [ObservableProperty]
    private string _listTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon), nameof(PlayPauseLabel))]
    private bool _isPlaying;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private double _lengthSeconds;

    [ObservableProperty]
    private TimeSpan _position;

    [ObservableProperty]
    private TimeSpan _length;

    public NowPlayingViewModel(IAudioPlayer player, ITrackStreamSource streams, ILogger<NowPlayingViewModel> logger)
        : base(logger)
    {
        (_player, _streams) = (player, streams);
        _timer = new DispatcherTimer { Interval = TickInterval };
        _timer.Tick += (_, _) => UpdateFromPlayer();
        player.Ended += (_, _) => Dispatcher.UIThread.Post(OnEnded);
        player.Failed += (_, _) => Dispatcher.UIThread.Post(OnFailed);
    }

    public bool HasCurrent => Current is not null;

    /// <summary>The saved playlist being played, so playback can move to a Yoto player; null for local files.</summary>
    public Playlist? PlayingPlaylist { get; private set; }

    /// <summary>The bar shows while something plays, is loading, or failed to play.</summary>
    public bool ShowBar => HasCurrent || IsBusy || Error is not null;

    public string PlayPauseIcon => IsPlaying ? "IconPause" : "IconPlay";

    public string PlayPauseLabel => IsPlaying ? "Pause" : "Play";

    public double Volume
    {
        get => _player.Volume;
        set
        {
            _player.Volume = (int)Math.Round(value);
            OnPropertyChanged();
        }
    }

    /// <summary>True when <paramref name="rowKey"/> is the row being played (paused or not).</summary>
    public bool IsCurrentRow(string rowKey) => string.Equals(Current?.RowKey, rowKey, StringComparison.Ordinal);

    /// <summary>Plays a saved playlist in its current on-screen order, from the row with <paramref name="chapterKey"/>.</summary>
    public Task PlayPlaylistAsync(Playlist playlist, string chapterKey)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        if (playlist.CardId is not { } cardId)
        {
            return Task.CompletedTask;
        }

        return RunAsync("Loading track", async token =>
        {
            // Signed URLs expire, so they are fetched each time a list starts rather than kept.
            var streams = await _streams.GetStreamsAsync(cardId, token);
            var list = PlaybackLists.FromPlaylist(playlist, streams, chapterKey)
                ?? throw new InvalidOperationException("Yoto has no playable audio for this playlist yet. Try again in a minute.");
            Start(list, playlist.Title);
            PlayingPlaylist = playlist;
        });
    }

    /// <summary>Plays the local files of an import draft, from <paramref name="trackIndex"/>.</summary>
    public void PlayDraft(PlaylistDraft draft, int trackIndex)
    {
        ArgumentNullException.ThrowIfNull(draft);
        Start(PlaybackLists.FromDraft(draft, trackIndex), draft.Title);
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IsBusy) or nameof(Error))
        {
            OnPropertyChanged(nameof(ShowBar));
        }
    }

    partial void OnPositionSecondsChanged(double value)
    {
        if (!_isUpdatingPosition && Math.Abs(value - _player.Position.TotalSeconds) > SeekToleranceSeconds)
        {
            _player.Seek(TimeSpan.FromSeconds(value));
        }
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_list is null)
        {
            return;
        }

        _player.SetPaused(IsPlaying);
        IsPlaying = !IsPlaying;
    }

    [RelayCommand]
    private void Next()
    {
        if (_list?.MoveNext() == true)
        {
            PlayCurrent();
        }
    }

    [RelayCommand]
    private void Previous()
    {
        if (_list is null)
        {
            return;
        }

        if (_list.MovePrevious(_player.Position))
        {
            PlayCurrent();
        }
        else
        {
            _player.Seek(TimeSpan.Zero);
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _player.StopPlayback();
        _timer.Stop();
        (_list, PlayingPlaylist) = (null, null);
        (Current, IsPlaying, PositionSeconds, LengthSeconds, Position, Length) = (null, false, 0, 0, TimeSpan.Zero, TimeSpan.Zero);
    }

    private void Start(PlaybackList list, string title)
    {
        (_list, ListTitle, Error, PlayingPlaylist) = (list, title, null, null);
        PlayCurrent();
    }

    private void PlayCurrent()
    {
        if (_list is null)
        {
            return;
        }

        try
        {
            _player.Play(_list.Current.Source);
        }
        catch (AudioPlaybackUnavailableException exception)
        {
            Logger.LogError(exception, "Audio engine unavailable");
            Stop();
            Error = exception.Message;
            return;
        }

        (Current, IsPlaying, Length, LengthSeconds) = (_list.Current, true, _list.Current.Duration, _list.Current.Duration.TotalSeconds);
        _timer.Start();
    }

    private void UpdateFromPlayer()
    {
        var length = _player.Length > TimeSpan.Zero ? _player.Length : Current?.Duration ?? TimeSpan.Zero;
        _isUpdatingPosition = true;
        (Position, PositionSeconds, Length, LengthSeconds) = (_player.Position, _player.Position.TotalSeconds, length, length.TotalSeconds);
        _isUpdatingPosition = false;
    }

    private void OnEnded()
    {
        if (_list?.MoveNext() == true)
        {
            PlayCurrent();
            return;
        }

        Stop();
    }

    private void OnFailed()
    {
        var title = Current?.Title;
        Stop();
        Error = $"Couldn't play “{title}”. Check your internet connection, or that the file still exists.";
    }
}
