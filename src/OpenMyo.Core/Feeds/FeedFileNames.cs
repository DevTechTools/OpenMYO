using System.Globalization;
using OpenMyo.Core.Import;

namespace OpenMyo.Core.Feeds;

/// <summary>Picks local file names for downloaded episodes and artwork, which the publisher reads types from.</summary>
public static class FeedFileNames
{
    private const string DefaultAudioExtension = ".mp3";
    private const string DefaultImageExtension = ".jpg";

    private static readonly Dictionary<string, string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["audio/mpeg"] = ".mp3",
        ["audio/mp3"] = ".mp3",
        ["audio/mp4"] = ".m4a",
        ["audio/x-m4a"] = ".m4a",
        ["audio/m4a"] = ".m4a",
        ["audio/aac"] = ".aac",
        ["audio/x-aac"] = ".aac",
        ["audio/ogg"] = ".ogg",
        ["audio/opus"] = ".opus",
        ["audio/flac"] = ".flac",
        ["audio/wav"] = ".wav",
        ["audio/x-wav"] = ".wav",
    };

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] GifSignature = [0x47, 0x49, 0x46, 0x38];

    /// <summary>"001.mp3": numbered in listening order, extension from the URL, else the declared type, else mp3.</summary>
    public static string ForEpisode(FeedEpisode episode, int index)
    {
        ArgumentNullException.ThrowIfNull(episode);
        var fromUrl = Path.GetExtension(episode.AudioUrl.AbsolutePath);
        var extension = MediaTypes.IsAudio(fromUrl) ? fromUrl.ToLowerInvariant()
            : AudioExtensions.GetValueOrDefault(MimeOnly(episode.ContentType)) ?? DefaultAudioExtension;
        return (index + 1).ToString("000", CultureInfo.InvariantCulture) + extension;
    }

    /// <summary>"cover.png" etc., from the picture's first bytes, since feed image URLs often have no extension.</summary>
    public static string ForCover(ReadOnlySpan<byte> header) =>
        "cover" + (header.StartsWith(PngSignature) ? ".png" : header.StartsWith(GifSignature) ? ".gif" : DefaultImageExtension);

    private static string MimeOnly(string? contentType) => contentType?.Split(';')[0].Trim() ?? string.Empty;
}
