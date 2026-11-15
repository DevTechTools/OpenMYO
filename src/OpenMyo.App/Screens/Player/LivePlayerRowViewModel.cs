using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Converters;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// One Yoto player that is playing (or paused) while it is not the chosen output, shown as a line in the bar:
/// "Kitchen Yoto · The Gruffalo · 1:23 / 4:00", with pause/resume, volume and Control.
/// </summary>
public sealed partial class LivePlayerRowViewModel : ObservableObject
{
    private static readonly TimeSpan VolumeDelay = TimeSpan.FromMilliseconds(300);

    private readonly IPlayerRemote _remote;
    private readonly ILogger _logger;
    private readonly Action<YotoPlayer> _control;
    private readonly DispatcherTimer _volumeDelay;
    private bool _isApplyingState;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon), nameof(PlayPauseLabel), nameof(PlayPauseAccessibleName))]
    private bool _isPlaying;

    [ObservableProperty]
    private string _trackTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeText))]
    private TimeSpan _position;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeText))]
    private TimeSpan _length;

    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private double _volumeMax = PlayerState.VolumeSteps;

    public LivePlayerRowViewModel(YotoPlayer player, IPlayerRemote remote, ILogger logger, Action<YotoPlayer> control)
    {
        (Player, _remote, _logger, _control) = (player, remote, logger, control);
        _volumeDelay = new DispatcherTimer { Interval = VolumeDelay };
        _volumeDelay.Tick += (_, _) => SendVolume();
    }

    public YotoPlayer Player { get; }

    public string Name => Player.Name;

    public string TimeText => Length > TimeSpan.Zero
        ? $"{Time(Position)} / {Time(Length)}"
        : Time(Position);

    public string PlayPauseIcon => IsPlaying ? "IconPause" : "IconPlay";

    public string PlayPauseLabel => IsPlaying ? "Pause" : "Resume";

    public string PlayPauseAccessibleName => $"{PlayPauseLabel} {Name}";

    public string ControlAccessibleName => $"Control {Name} from the main player";

    public string VolumeAccessibleName => $"Volume on {Name}";

    /// <summary>Shows what the player last reported.</summary>
    public void Apply(PlayerState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _isApplyingState = true;
        IsPlaying = state.Status == PlayerStatus.Playing;
        TrackTitle = state.TrackTitle ?? "Playing";
        (Length, Position) = (state.Length, state.Position);
        VolumeMax = state.VolumeMax ?? PlayerState.VolumeSteps;
        if (state.Volume is { } volume)
        {
            Volume = volume;
        }

        _isApplyingState = false;
    }

    /// <summary>Moves the position on between the player's reports.</summary>
    public void Tick(TimeSpan by)
    {
        if (IsPlaying && (Length <= TimeSpan.Zero || Position + by <= Length))
        {
            Position += by;
        }
    }

    [RelayCommand]
    private Task PlayPauseAsync()
    {
        var command = IsPlaying ? PlayerCommand.Pause : PlayerCommand.Resume;
        IsPlaying = !IsPlaying; // Answer the click at once; the player's next report confirms it.
        return SendAsync(token => _remote.SendAsync(Player.DeviceId, command, token), "pausing a Yoto player");
    }

    [RelayCommand]
    private void Control() => _control(Player);

    partial void OnVolumeChanged(double value)
    {
        if (!_isApplyingState)
        {
            _volumeDelay.Stop();
            _volumeDelay.Start();
        }
    }

    private void SendVolume()
    {
        _volumeDelay.Stop();
        var level = (int)Math.Round(Volume);
        SendAsync(token => _remote.SetVolumeAsync(Player.DeviceId, level, token), "setting a Yoto player's volume")
            .Forget(_logger, "setting a Yoto player's volume");
    }

    private static string Time(TimeSpan span) => DurationConverter.Format(span, CultureInfo.CurrentCulture);

    private async Task SendAsync(Func<CancellationToken, Task> send, string what)
    {
        try
        {
            await send(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // UI boundary: a missed command on a side line is logged; the player's next report shows the truth.
            _logger.LogWarning(ex, "Failed {What}", what);
        }
    }
}
