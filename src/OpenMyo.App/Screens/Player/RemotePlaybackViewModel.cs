using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Playback;
using OpenMyo.Core.Players;
using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// The now-playing bar while a Yoto player is the output: shows what the player reports and sends it commands.
/// Players report changes rather than a steady clock, so the position ticks on locally between reports.
/// A player takes a few seconds to fetch and start a track, so the bar shows the requested track as "Starting"
/// straight away and switches to the player's own state once it confirms.
/// </summary>
public sealed partial class RemotePlaybackViewModel : OperationViewModel
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan VolumeDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan RestartThreshold = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan FirstStateTimeout = TimeSpan.FromSeconds(4);

    private readonly IPlayerRemote _remote;
    private readonly DispatcherTimer _tick;
    private readonly DispatcherTimer _volumeDelay;
    private readonly DispatcherTimer _startTimeout;
    private Playlist? _playlist;
    private PlayerState _state = PlayerState.Empty;
    private TaskCompletionSource? _firstState;
    private bool _isApplyingState;
    private bool _isSeeking;

    /// <summary>What was asked to play and has not been confirmed by the player yet.</summary>
    private PlayTarget? _pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdleText))]
    private YotoPlayer? _player;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon), nameof(PlayPauseLabel))]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTrack))]
    private string? _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    private string? _listTitle;

    /// <summary>"Connecting to …" or "Starting on …" while waiting for the player; null otherwise.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending), nameof(Subtitle), nameof(IdleText))]
    private string? _pendingText;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private double _lengthSeconds;

    [ObservableProperty]
    private TimeSpan _position;

    [ObservableProperty]
    private TimeSpan _length;

    /// <summary>Volume in player steps; the slider runs to <see cref="VolumeMax"/>, the player's parental limit.</summary>
    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private double _volumeMax = PlayerState.VolumeSteps;

    public RemotePlaybackViewModel(IPlayerRemote remote, ILogger<RemotePlaybackViewModel> logger)
        : base(logger)
    {
        _remote = remote;
        _tick = new DispatcherTimer { Interval = TickInterval };
        _tick.Tick += (_, _) => Advance();
        _volumeDelay = new DispatcherTimer { Interval = VolumeDelay };
        _volumeDelay.Tick += (_, _) => SendVolume();
        _startTimeout = new DispatcherTimer { Interval = StartTimeout };
        _startTimeout.Tick += (_, _) => OnStartTimedOut();
        remote.StateChanged += (_, e) => Dispatcher.UIThread.Post(() => OnStateChanged(e));
    }

    /// <summary>Raised when the player stops answering; the bar falls back to this computer.</summary>
    public event EventHandler<YotoPlayer>? PlayerLost;

    public bool HasTrack => Title is not null;

    public bool IsPending => PendingText is not null;

    /// <summary>True when the player is playing or paused on something, which switching to it must not override.</summary>
    public bool IsPlayerBusy => _state.IsBusy;

    /// <summary>Under the title: what is happening while waiting, otherwise the playlist.</summary>
    public string? Subtitle => PendingText ?? ListTitle;

    /// <summary>Shown when no track is up: connecting, or ready for the next ▶.</summary>
    public string IdleText => PendingText ?? $"Ready on {Player?.Name}. Press ▶ on any track to play it there.";

    public string PlayPauseIcon => IsPlaying ? "IconPause" : "IconPlay";

    public string PlayPauseLabel => IsPlaying ? "Pause" : "Resume";

    /// <summary>
    /// Row key of the chapter the player is on, so that row in the editor shows it is playing. The row asked for
    /// shows straight away, before the player confirms, so the click has a visible effect.
    /// </summary>
    public string? CurrentRowKey => Player is null ? null
        : _pending is { } pending ? PlaybackLists.RowKey(pending.CardId, pending.ChapterKey)
        : _state is { HasCard: true, CardId: { } card, ChapterKey: { } chapter } ? PlaybackLists.RowKey(card, chapter)
        : null;

    /// <summary>
    /// Makes <paramref name="player"/> the one this bar shows. Connects (or reuses the open connection) and waits
    /// briefly for its first report, so <see cref="IsPlayerBusy"/> is known. Throws when it cannot be reached.
    /// </summary>
    public async Task AttachAsync(YotoPlayer player, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);
        Reset();
        Player = player;
        PendingText = $"Connecting to {player.Name}…";
        var firstState = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstState = firstState;
        try
        {
            await _remote.ConnectAsync(player.DeviceId, cancellationToken);
            if (_remote.LastState(player.DeviceId) is { } known && known != PlayerState.Empty)
            {
                OnState(known);
            }

            await Task.WhenAny(firstState.Task, Task.Delay(FirstStateTimeout, cancellationToken));
        }
        catch
        {
            Player = null;
            throw;
        }
        finally
        {
            _firstState = null;
            PendingText = _pending is null ? null : PendingText;
        }
    }

    /// <summary>Stops showing the player. Its connection stays open so the playing-players list keeps watching it.</summary>
    public void Detach()
    {
        Player = null;
        Reset();
    }

    /// <summary>Starts <paramref name="playlist"/> on the player at <paramref name="chapterKey"/>.</summary>
    public Task PlayAsync(Playlist playlist, string chapterKey, TimeSpan startAt)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        var target = PlayTargets.ForChapter(playlist, chapterKey, startAt)
            ?? throw new InvalidOperationException("Save this playlist first. A Yoto player can only play playlists saved to Yoto.");
        _playlist = playlist;
        ShowStarting(target, playlist);
        return SendStartAsync(target);
    }

    /// <summary>
    /// Seeks to <paramref name="seconds"/>. The view calls this only when the user lets go of the slider: the
    /// slider also moves by itself as the length changes, and treating that as a seek restarted the track in a loop.
    /// </summary>
    public void SeekTo(double seconds)
    {
        _isSeeking = false;
        if (IsPending || Length <= TimeSpan.Zero || Math.Abs(seconds - Position.TotalSeconds) < 1)
        {
            return;
        }

        RestartAsync(TimeSpan.FromSeconds(seconds)).Forget(Logger, "seeking on a Yoto player");
    }

    /// <summary>The user has pressed the slider: the position stops following the player until they let go.</summary>
    public void BeginSeek() => _isSeeking = true;

    [RelayCommand]
    private Task PlayPauseAsync() => Send(IsPlaying ? PlayerCommand.Pause : PlayerCommand.Resume);

    [RelayCommand]
    private Task NextAsync() => MoveAsync(1);

    /// <summary>Like a CD player: back to the start of this track, or to the previous one when near its start.</summary>
    [RelayCommand]
    private Task PreviousAsync() => Position > RestartThreshold ? RestartAsync(TimeSpan.Zero) : MoveAsync(-1);

    [RelayCommand]
    private Task StopAsync()
    {
        var command = Send(PlayerCommand.Stop);
        Reset();
        return command;
    }

    partial void OnVolumeChanged(double value)
    {
        if (!_isApplyingState)
        {
            // Wait until the slider settles rather than sending a command for every step of a drag.
            _volumeDelay.Stop();
            _volumeDelay.Start();
        }
    }

    private async Task SendStartAsync(PlayTarget target)
    {
        await RunAsync($"Starting on {Player?.Name}", token => _remote.PlayAsync(DeviceId(), target, token));
        if (Error is not null && _pending == target)
        {
            ClearPending();
        }
    }

    /// <summary>Shows the requested track at once, marked as starting, until the player reports it playing.</summary>
    private void ShowStarting(PlayTarget target, Playlist playlist)
    {
        _pending = target;
        _isApplyingState = true;
        (Title, ListTitle, IsPlaying) = (ChapterTitle(target.ChapterKey), playlist.Title, false);
        (Position, PositionSeconds) = (target.StartAt, target.StartAt.TotalSeconds);
        _isApplyingState = false;
        _tick.Stop();
        PendingText = $"Starting on {Player?.Name}…";
        _startTimeout.Stop();
        _startTimeout.Start();
        OnPropertyChanged(nameof(CurrentRowKey));
    }

    private void ClearPending()
    {
        _pending = null;
        _startTimeout.Stop();
        PendingText = null;
        Apply(_state);
        OnPropertyChanged(nameof(CurrentRowKey));
    }

    private void OnStartTimedOut()
    {
        Error = $"{Player?.Name} didn't start playing. Check it's switched on and awake, then try again.";
        ClearPending();
    }

    private Task Send(PlayerCommand command)
    {
        var deviceId = DeviceId();
        return RunAsync("Sending to " + Player?.Name, token => _remote.SendAsync(deviceId, command, token));
    }

    private string DeviceId() => Player?.DeviceId ?? throw new InvalidOperationException("Choose a Yoto player first.");

    private Task MoveAsync(int offset) =>
        KnownPosition() is (Playlist playlist, string chapter) && PlayTargets.Neighbour(playlist, chapter, offset) is { } key
            ? PlayAsync(playlist, key, TimeSpan.Zero)
            : Task.CompletedTask;

    private Task RestartAsync(TimeSpan at) =>
        KnownPosition() is (Playlist playlist, string chapter) ? PlayAsync(playlist, chapter, at) : Task.CompletedTask;

    /// <summary>The playlist and chapter the player is on, when it is the playlist OpenMyo started (so neighbours are known).</summary>
    private (Playlist Playlist, string Chapter)? KnownPosition() =>
        _playlist is { } playlist && _state.ChapterKey is { } chapter && string.Equals(_state.CardId, playlist.CardId, StringComparison.Ordinal)
            ? (playlist, chapter)
            : null;

    private void SendVolume()
    {
        _volumeDelay.Stop();
        if (Player is not { } player)
        {
            return;
        }

        var level = (int)Math.Round(Volume);
        RunAsync("Setting volume", token => _remote.SetVolumeAsync(player.DeviceId, level, token)).Forget(Logger, "setting Yoto volume");
    }

    private void OnStateChanged(PlayerStateChangedEventArgs e)
    {
        if (Player is not { } player || e.DeviceId != player.DeviceId)
        {
            return;
        }

        if (!e.IsConnected)
        {
            PlayerLost?.Invoke(this, player);
            return;
        }

        OnState(e.State);
    }

    private void OnState(PlayerState state)
    {
        _state = state;
        _firstState?.TrySetResult();
        OnPropertyChanged(nameof(IsPlayerBusy));
        if (_pending is { } pending)
        {
            // Until the player confirms the new track, keep showing it as starting; volume still follows the player.
            if (state.Status != PlayerStatus.Playing || !string.Equals(state.CardId, pending.CardId, StringComparison.Ordinal))
            {
                ApplyVolume(state);
                return;
            }

            ClearPending();
            return;
        }

        Apply(state);
        OnPropertyChanged(nameof(CurrentRowKey));
    }

    private void Apply(PlayerState state)
    {
        _isApplyingState = true;
        IsPlaying = state.Status == PlayerStatus.Playing;
        Title = state.IsBusy ? state.TrackTitle ?? ChapterTitle(state.ChapterKey) : null;
        if (!string.Equals(state.CardId, _playlist?.CardId, StringComparison.Ordinal))
        {
            ListTitle = null;
        }

        // Length before position, so the slider never has to clamp a position beyond its old maximum.
        (Length, LengthSeconds) = (state.Length, state.Length.TotalSeconds);
        Position = state.Position;
        if (!_isSeeking)
        {
            PositionSeconds = state.Position.TotalSeconds;
        }

        _isApplyingState = false;
        ApplyVolume(state);
        if (IsPlaying)
        {
            _tick.Start();
        }
        else
        {
            _tick.Stop();
        }
    }

    private void ApplyVolume(PlayerState state)
    {
        _isApplyingState = true;
        VolumeMax = state.VolumeMax ?? PlayerState.VolumeSteps;
        if (state.Volume is { } volume)
        {
            Volume = volume;
        }

        _isApplyingState = false;
    }

    private string? ChapterTitle(string? chapterKey) =>
        _playlist?.Chapters.FirstOrDefault(chapter => chapter.Key == chapterKey)?.Title;

    private void Advance()
    {
        if (Position + TickInterval > Length && Length > TimeSpan.Zero)
        {
            return;
        }

        Position += TickInterval;
        if (!_isSeeking)
        {
            PositionSeconds = Position.TotalSeconds;
        }
    }

    private void Reset()
    {
        _tick.Stop();
        _startTimeout.Stop();
        _pending = null;
        PendingText = null;
        _state = PlayerState.Empty;
        Apply(PlayerState.Empty);
        OnPropertyChanged(nameof(CurrentRowKey));
        OnPropertyChanged(nameof(IsPlayerBusy));
    }
}
