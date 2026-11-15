using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.Infrastructure.Audio;

/// <summary>
/// Plays audio through LibVLC. The native engine loads on first use, so start-up stays fast and a machine without
/// it can still use the rest of the app. LibVLC raises events on its own threads and deadlocks if called back from
/// them, so the handlers here only forward the event.
/// </summary>
public sealed partial class VlcAudioPlayer(ILogger<VlcAudioPlayer> logger) : IAudioPlayer
{
    private const int DefaultVolume = 80;

    // Yoto's media CDN rejects requests without a recognisable User-Agent.
    private const string UserAgentOption = ":http-user-agent=OpenMyo/1.0";

    private readonly Lock _gate = new();
    private LibVLC? _engine;
    private MediaPlayer? _player;
    private int _volume = DefaultVolume;
    private bool _isDisposed;

    public event EventHandler? Ended;

    public event EventHandler? Failed;

    public bool IsPlaying => Read(player => player.IsPlaying, false);

    public TimeSpan Position => Read(player => player.Time > 0 ? TimeSpan.FromMilliseconds(player.Time) : TimeSpan.Zero, TimeSpan.Zero);

    public TimeSpan Length => Read(player => player.Length > 0 ? TimeSpan.FromMilliseconds(player.Length) : TimeSpan.Zero, TimeSpan.Zero);

    public int Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            Use(player => player.Volume = _volume);
        }
    }

    public void Play(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (engine, player) = EnsureStarted();
        using var media = new Media(engine, source, UserAgentOption);
        player.Play(media);
        player.Volume = _volume;
    }

    public void SetPaused(bool paused) => Use(player => player.SetPause(paused));

    public void StopPlayback() => Use(player => player.Stop());

    public void Seek(TimeSpan position) => Use(player =>
    {
        if (player.IsSeekable)
        {
            player.Time = (long)position.TotalMilliseconds;
        }
    });

    /// <summary>
    /// Shutdown disposes services asynchronously while the UI keeps running, so the player bar's timer can still
    /// ask for the position. Clearing the player under the same lock makes those late calls return defaults
    /// instead of reaching into a freed native player (which crashes the process).
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _isDisposed = true;
            _player?.Dispose();
            _engine?.Dispose();
            (_player, _engine) = (null, null);
        }
    }

    private void Use(Action<MediaPlayer> action)
    {
        lock (_gate)
        {
            if (_player is not null)
            {
                action(_player);
            }
        }
    }

    /// <summary>Runs <paramref name="action"/> on the player if one is running; otherwise returns <paramref name="fallback"/>.</summary>
    private T Read<T>(Func<MediaPlayer, T> action, T fallback)
    {
        lock (_gate)
        {
            return _player is null ? fallback : action(_player);
        }
    }

    private (LibVLC Engine, MediaPlayer Player) EnsureStarted()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            if (_engine is not null && _player is not null)
            {
                return (_engine, _player);
            }

            try
            {
                LibVLCSharp.Shared.Core.Initialize();
                _engine = new LibVLC("--no-video", "--quiet");
                _player = new MediaPlayer(_engine);
            }
            catch (Exception exception) when (exception is VLCException or DllNotFoundException or BadImageFormatException)
            {
                LogEngineFailed(exception);
                throw new AudioPlaybackUnavailableException(
                    "The audio engine (LibVLC) could not be loaded, so tracks can't be played. Reinstalling OpenMyo should fix this.",
                    exception);
            }

            _player.EndReached += (_, _) => Ended?.Invoke(this, EventArgs.Empty);
            _player.EncounteredError += (_, _) => Failed?.Invoke(this, EventArgs.Empty);
            return (_engine, _player);
        }
    }

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "Could not start LibVLC.")]
    private partial void LogEngineFailed(Exception exception);
}
