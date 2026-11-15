using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Import;

/// <summary>An audio file paired with the tags read from it.</summary>
public sealed record ScannedFile(string Path, AudioMetadata Metadata);
