namespace OpenMyo.Core.Abstractions;

/// <summary>The audio engine is missing or failed to start, so nothing can be played on this machine.</summary>
public sealed class AudioPlaybackUnavailableException : Exception
{
    public AudioPlaybackUnavailableException()
    {
    }

    public AudioPlaybackUnavailableException(string message)
        : base(message)
    {
    }

    public AudioPlaybackUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
