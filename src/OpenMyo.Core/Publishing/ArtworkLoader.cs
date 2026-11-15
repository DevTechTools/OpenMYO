using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Import;

namespace OpenMyo.Core.Publishing;

/// <summary>Loads the bytes behind an <see cref="ArtworkSource"/>.</summary>
public sealed class ArtworkLoader(IAudioMetadataReader metadataReader)
{
    public async Task<EmbeddedPicture?> LoadAsync(ArtworkSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.IsEmbedded)
        {
            return await Task.Run(() => metadataReader.ReadPicture(source.Path), cancellationToken);
        }

        var bytes = await File.ReadAllBytesAsync(source.Path, cancellationToken);
        return new EmbeddedPicture(bytes, MediaTypes.ContentTypeOf(source.Path));
    }
}
