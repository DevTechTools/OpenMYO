namespace OpenMyo.Core.Import;

/// <summary>File extensions OpenMyo understands and their MIME types.</summary>
public static class MediaTypes
{
    private static readonly Dictionary<string, string> AudioTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".m4b"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".flac"] = "audio/flac",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/ogg",
        [".aif"] = "audio/aiff",
        [".aiff"] = "audio/aiff",
    };

    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
    };

    public static IReadOnlyCollection<string> AudioExtensions => AudioTypes.Keys;

    public static IReadOnlyCollection<string> ImageExtensions => ImageTypes.Keys;

    public static bool IsAudio(string path) => AudioTypes.ContainsKey(Path.GetExtension(path));

    public static bool IsImage(string path) => ImageTypes.ContainsKey(Path.GetExtension(path));

    /// <summary>MIME type for an audio or image path; <c>application/octet-stream</c> when unknown.</summary>
    public static string ContentTypeOf(string path)
    {
        var extension = Path.GetExtension(path);
        return AudioTypes.GetValueOrDefault(extension) ?? ImageTypes.GetValueOrDefault(extension) ?? "application/octet-stream";
    }
}
