namespace OpenMyo.Core.Abstractions;

/// <summary>Reads tags and embedded artwork from local audio files.</summary>
public interface IAudioMetadataReader
{
    /// <summary>Reads tags. Never throws for unreadable tags; returns empty metadata instead.</summary>
    AudioMetadata Read(string filePath);

    /// <summary>Loads the embedded front cover, or null when the file has none.</summary>
    EmbeddedPicture? ReadPicture(string filePath);
}

/// <param name="PictureHash">Stable hash of the embedded picture bytes so callers can compare art cheaply; null when none.</param>
public sealed record AudioMetadata(
    string? Title,
    string? Album,
    uint? TrackNumber,
    uint? DiscNumber,
    TimeSpan Duration,
    string? PictureHash)
{
    public static AudioMetadata Empty { get; } = new(null, null, null, null, TimeSpan.Zero, null);
}

public sealed record EmbeddedPicture(ReadOnlyMemory<byte> Data, string ContentType);
