using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// Watches the user's online Yoto players from sign-in, so the bar can show any that are already playing, even
/// before anything is played from the app. Watching only listens; it never changes what a player is doing.
/// The player chosen as the output is left out, because the main part of the bar already shows it.
/// </summary>
public sealed partial class LivePlayersViewModel : ObservableObject
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    // Players switched on later appear within this time; the output menu also refreshes the list when opened.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(2);

    private readonly IPlayerDirectory _directory;
    private readonly IPlayerRemote _remote;
    private readonly ILogger<LivePlayersViewModel> _logger;
    private readonly Dictionary<string, YotoPlayer> _players = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _tick;
    private readonly DispatcherTimer _refresh;
    private string? _excludedDeviceId;
    private bool _isWatching;

    public LivePlayersViewModel(IPlayerDirectory directory, IPlayerRemote remote, ILogger<LivePlayersViewModel> logger)
    {
        (_directory, _remote, _logger) = (directory, remote, logger);
        _tick = new DispatcherTimer { Interval = TickInterval };
        _tick.Tick += (_, _) => Tick();
        _refresh = new DispatcherTimer { Interval = RefreshInterval };
        _refresh.Tick += (_, _) => RefreshAsync().Forget(_logger, "looking for Yoto players");
        remote.StateChanged += (_, e) => Dispatcher.UIThread.Post(() => OnStateChanged(e));
    }

    /// <summary>Raised by a row's Control button; the bar makes that player the output.</summary>
    public event EventHandler<YotoPlayer>? ControlRequested;

    /// <summary>Players that are playing or paused, other than the output.</summary>
    public ObservableCollection<LivePlayerRowViewModel> Rows { get; } = [];

    public bool HasRows => Rows.Count > 0;

    /// <summary>Starts watching every online player. Safe to call again; players already watched are kept.</summary>
    public Task StartAsync()
    {
        _isWatching = true;
        _refresh.Start();
        _tick.Start();
        return RefreshAsync();
    }

    /// <summary>Signing out: stops watching and closes every player connection.</summary>
    public async Task StopAsync()
    {
        _isWatching = false;
        _refresh.Stop();
        _tick.Stop();
        _players.Clear();
        Rows.Clear();
        OnPropertyChanged(nameof(HasRows));
        await _remote.DisconnectAllAsync(CancellationToken.None);
    }

    /// <summary>Leaves the chosen output out of the list (the main bar shows it) and puts back the previous one.</summary>
    public void Exclude(YotoPlayer? output)
    {
        _excludedDeviceId = output?.DeviceId;
        foreach (var deviceId in _players.Keys)
        {
            Show(deviceId, _remote.LastState(deviceId));
        }
    }

    private async Task RefreshAsync()
    {
        IReadOnlyList<YotoPlayer> players;
        try
        {
            players = await _directory.ListAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // UI boundary: background watching; the next refresh tries again.
            _logger.LogInformation(ex, "Could not list Yoto players");
            return;
        }

        foreach (var player in players.Where(player => player.IsOnline && _isWatching))
        {
            _players[player.DeviceId] = player;
            await WatchAsync(player);
        }
    }

    private async Task WatchAsync(YotoPlayer player)
    {
        try
        {
            await _remote.ConnectAsync(player.DeviceId, CancellationToken.None);
            Show(player.DeviceId, _remote.LastState(player.DeviceId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // UI boundary: one unreachable player must not stop the others being watched.
            LogNotWatched(_logger, ex, player.Name);
        }
    }

    private void OnStateChanged(PlayerStateChangedEventArgs e)
    {
        if (!_isWatching || !_players.ContainsKey(e.DeviceId))
        {
            return;
        }

        if (!e.IsConnected)
        {
            // Gone (switched off or out of Wi-Fi); the next refresh picks it up again if it comes back.
            _players.Remove(e.DeviceId);
            Show(e.DeviceId, state: null);
            return;
        }

        Show(e.DeviceId, e.State);
    }

    /// <summary>Adds, updates or removes the player's row to match <paramref name="state"/>.</summary>
    private void Show(string deviceId, PlayerState? state)
    {
        var row = Rows.FirstOrDefault(r => r.Player.DeviceId == deviceId);
        var visible = state is { IsBusy: true } && deviceId != _excludedDeviceId && _players.ContainsKey(deviceId);
        if (!visible)
        {
            if (row is not null)
            {
                Rows.Remove(row);
                OnPropertyChanged(nameof(HasRows));
            }

            return;
        }

        if (row is null)
        {
            row = new LivePlayerRowViewModel(_players[deviceId], _remote, _logger, player => ControlRequested?.Invoke(this, player));
            Rows.Add(row);
            OnPropertyChanged(nameof(HasRows));
        }

        // Not null: visible requires a busy state.
        row.Apply(state!);
    }

    private void Tick()
    {
        foreach (var row in Rows)
        {
            row.Tick(TickInterval);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Could not watch Yoto player {Player}")]
    private static partial void LogNotWatched(ILogger logger, Exception exception, string player);
}
