namespace OpenMyo.Core.Import;

/// <summary>Where a picture comes from: an image file, or the art embedded in an audio file.</summary>
/// <param name="Path">Image file path, or the audio file path when <paramref name="IsEmbedded"/> is true.</param>
public sealed record ArtworkSource(string Path, bool IsEmbedded)
{
    public static ArtworkSource File(string imagePath) => new(imagePath, IsEmbedded: false);

    public static ArtworkSource Embedded(string audioPath) => new(audioPath, IsEmbedded: true);
}
