using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Tests.Fakes;

public sealed class FakeAudioMetadataReader : IAudioMetadataReader
{
    public Dictionary<string, AudioMetadata> Tags { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, EmbeddedPicture> Pictures { get; } = new(StringComparer.Ordinal);

    public AudioMetadata Read(string filePath) => Tags.GetValueOrDefault(filePath) ?? AudioMetadata.Empty;

    public EmbeddedPicture? ReadPicture(string filePath) => Pictures.GetValueOrDefault(filePath);
}
