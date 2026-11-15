namespace OpenMyo.Core.Playlists;

/// <summary>A playable audio item inside a chapter.</summary>
public sealed record Track
{
    public required string Key { get; init; }

    public required string Title { get; init; }

    /// <summary>Yoto reference to the transcoded audio, e.g. <c>yoto:#sha256</c>, or a stream URL.</summary>
    public required string TrackUrl { get; init; }

    /// <summary>Audio container reported by Yoto after transcoding (e.g. <c>mp3</c>, <c>aac</c>).</summary>
    public string Format { get; init; } = "aac";

    /// <summary><c>audio</c> for uploaded files, <c>stream</c> for live streams.</summary>
    public string Type { get; init; } = "audio";

    public TimeSpan Duration { get; init; }

    public long FileSize { get; init; }

    /// <summary><c>stereo</c> or <c>mono</c>; null when unknown.</summary>
    public string? Channels { get; init; }

    public string? IconMediaId { get; init; }
}
