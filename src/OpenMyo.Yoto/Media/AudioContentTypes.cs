namespace OpenMyo.Yoto.Media;

internal static class AudioContentTypes
{
    private const string Fallback = "application/octet-stream";

    public static string ForFile(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".mp3" => "audio/mpeg",
        ".m4a" => "audio/mp4",
        ".aac" => "audio/aac",
        ".flac" => "audio/flac",
        ".wav" => "audio/wav",
        ".ogg" or ".opus" => "audio/ogg",
        ".aif" or ".aiff" => "audio/aiff",
        _ => Fallback,
    };
}
