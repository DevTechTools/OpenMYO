using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.Editor;
using OpenMyo.App.Screens.Import;
using OpenMyo.App.Services;
using OpenMyo.Core.Playback;
using OpenMyo.Core.Players;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// The bar along the bottom of the window. Plays on this computer (<see cref="Local"/>) or on a Yoto player
/// (<see cref="Remote"/>), as chosen in <see cref="Output"/>. Like choosing a speaker on a phone, changing the
/// output moves what is playing there, at the same track and position.
/// </summary>
public sealed partial class PlayerBarViewModel : ObservableObject
{
    private readonly ILogger<PlayerBarViewModel> _logger;

    [ObservableProperty]
    private string? _notice;

    public PlayerBarViewModel(
        NowPlayingViewModel local,
        RemotePlaybackViewModel remote,
        OutputPickerViewModel output,
        LivePlayersViewModel live,
        ILogger<PlayerBarViewModel> logger)
    {
        (Local, Remote, Output, Live, _logger) = (local, remote, output, live, logger);
        Live.ControlRequested += (_, player) => ControlAsync(player).Forget(_logger, "taking control of a Yoto player");
        Output.OutputChosen += (_, player) => SwitchOutputAsync(player).Forget(_logger, "changing where playback goes");
        Remote.PlayerLost += (_, player) => FallBackAsync(player, $"{player.Name} stopped responding, so playback moved back to this computer.")
            .Forget(_logger, "leaving an unreachable Yoto player");
        Local.PropertyChanged += OnChildChanged;
        Remote.PropertyChanged += OnChildChanged;
        Output.PropertyChanged += OnChildChanged;
        Live.PropertyChanged += OnChildChanged;
    }

    public NowPlayingViewModel Local { get; }

    public RemotePlaybackViewModel Remote { get; }

    public OutputPickerViewModel Output { get; }

    /// <summary>Other Yoto players that are playing, each on its own line above the controls.</summary>
    public LivePlayersViewModel Live { get; }

    public bool IsRemote => Output.IsRemote;

    /// <summary>Local controls show once something has played here; until then the bar may hold only playing Yotos.</summary>
    public bool ShowLocal => !IsRemote && Local.ShowBar;

    /// <summary>
    /// Shown while something plays here or on any Yoto player, and always while a Yoto player is chosen, so the
    /// choice is visible.
    /// </summary>
    public bool ShowBar => IsRemote || Local.ShowBar || Live.HasRows || Notice is not null;

    /// <summary>The row playing wherever playback is, so it can show the playing marker.</summary>
    public string? CurrentRowKey => IsRemote ? Remote.CurrentRowKey : Local.Current?.RowKey;

    /// <summary>A row's ▶: plays it where the output points, or pauses/resumes when it is the row already playing.</summary>
    public void Play(PlaylistPlayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (CurrentRowKey == PlaybackLists.RowKey(request.Playlist.CardId, request.ChapterKey))
        {
            PlayPauseCommand.Execute(null);
            return;
        }

        var playing = IsRemote
            ? Remote.PlayAsync(request.Playlist, request.ChapterKey, TimeSpan.Zero)
            : Local.PlayPlaylistAsync(request.Playlist, request.ChapterKey);
        playing.Forget(_logger, "starting playback");
    }

    /// <summary>Files being imported exist only on this computer, so they always play here.</summary>
    public void Play(DraftPlayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (CurrentRowKey == request.Draft.Tracks[request.TrackIndex].SourcePath)
        {
            Local.PlayPauseCommand.Execute(null);
            return;
        }

        Local.PlayDraft(request.Draft, request.TrackIndex);
        if (IsRemote)
        {
            FallBackAsync(Output.SelectedPlayer, "Files that aren't on Yoto yet play on this computer.")
                .Forget(_logger, "playing an import preview");
        }
    }

    [RelayCommand]
    private void PlayPause() => (IsRemote ? Remote.PlayPauseCommand : Local.PlayPauseCommand).Execute(null);

    [RelayCommand]
    private void Next() => (IsRemote ? Remote.NextCommand : Local.NextCommand).Execute(null);

    [RelayCommand]
    private void Previous() => (IsRemote ? Remote.PreviousCommand : Local.PreviousCommand).Execute(null);

    [RelayCommand]
    private void DismissNotice() => Notice = null;

    /// <summary>Signing in: starts watching the user's players so ones already playing show in the bar.</summary>
    public Task WatchPlayersAsync() => Live.StartAsync();

    /// <summary>Signing out stops local playback, returns to this computer and stops watching players.</summary>
    public async Task StopAllAsync()
    {
        Local.StopCommand.Execute(null);
        await FallBackAsync(Output.SelectedPlayer, notice: null);
        await Live.StopAsync();
    }

    /// <summary>A playing Yoto's Control button: makes it the output without changing what it plays.</summary>
    public Task ControlAsync(YotoPlayer player) => SwitchOutputAsync(player, moveLocal: false);

    /// <summary>
    /// Changing the output never changes what a Yoto player is doing. Only audio actually playing in this app moves
    /// across, and only to a player that is idle; a busy player carries on and the bar shows it.
    /// </summary>
    private async Task SwitchOutputAsync(YotoPlayer? player, bool moveLocal = true)
    {
        Notice = null;
        if (player is null)
        {
            await FallBackAsync(Output.SelectedPlayer, notice: null);
            return;
        }

        var playlist = moveLocal && Local.IsPlaying ? Local.PlayingPlaylist : null;
        var chapter = Local.Current?.RowKey is { } rowKey ? PlaybackLists.ChapterKeyOf(rowKey) : null;
        var position = Local.Position;

        // Show the player's bar ("Connecting to …") at once; local audio carries on until the player is ready.
        Output.SetOutput(player);
        try
        {
            await Remote.AttachAsync(player, CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // UI boundary: the player could not be reached, so go back to this computer and say why.
            _logger.LogWarning(ex, "Could not connect to Yoto player");
            Output.SetOutput(null);
            Notice = $"Couldn't reach {player.Name}. Check it is switched on and connected to Wi-Fi.";
            return;
        }

        if (playlist is null || chapter is null)
        {
            Local.StopCommand.Execute(null);
            return;
        }

        if (Remote.IsPlayerBusy)
        {
            Local.StopCommand.Execute(null);
            Notice = $"{player.Name} is already playing something, so it carries on. Press ▶ on a track to play it there instead.";
            return;
        }

        Local.StopCommand.Execute(null);
        await Remote.PlayAsync(playlist, chapter, position);
    }

    private Task FallBackAsync(YotoPlayer? from, string? notice)
    {
        Output.SetOutput(null);
        Notice = notice;
        if (from is not null)
        {
            Remote.Detach();
        }

        return Task.CompletedTask;
    }

    private void OnChildChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ShowBar));
        OnPropertyChanged(nameof(IsRemote));
        OnPropertyChanged(nameof(ShowLocal));
        if (e.PropertyName is nameof(NowPlayingViewModel.Current) or nameof(RemotePlaybackViewModel.CurrentRowKey)
            or nameof(OutputPickerViewModel.SelectedPlayer))
        {
            OnPropertyChanged(nameof(CurrentRowKey));
        }

        if (sender == Output && e.PropertyName == nameof(OutputPickerViewModel.SelectedPlayer))
        {
            Live.Exclude(Output.SelectedPlayer);
        }
    }

    partial void OnNoticeChanged(string? value) => OnPropertyChanged(nameof(ShowBar));
}
