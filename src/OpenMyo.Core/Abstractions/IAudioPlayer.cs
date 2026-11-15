namespace OpenMyo.Core.Abstractions;

/// <summary>
/// Plays one audio source at a time (an HTTPS stream or a local file). Implementations raise events on their own
/// threads; callers marshal to the UI thread and must not call back into the player from inside a handler.
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>The current source played to its end.</summary>
    event EventHandler? Ended;

    /// <summary>The current source could not be opened or decoded.</summary>
    event EventHandler? Failed;

    bool IsPlaying { get; }

    TimeSpan Position { get; }

    /// <summary>Length of the current source; zero until it is known.</summary>
    TimeSpan Length { get; }

    /// <summary>0 to 100.</summary>
    int Volume { get; set; }

    /// <summary>Starts <paramref name="source"/> from the beginning, replacing whatever was playing.</summary>
    /// <exception cref="AudioPlaybackUnavailableException">The audio engine could not be loaded on this machine.</exception>
    void Play(Uri source);

    /// <summary>Pauses or continues the current source.</summary>
    void SetPaused(bool paused);

    /// <summary>Stops and unloads the current source.</summary>
    void StopPlayback();

    void Seek(TimeSpan position);
}
