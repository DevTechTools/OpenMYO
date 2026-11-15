namespace OpenMyo.Core.Import;

/// <summary>Audio files that will become one playlist, before metadata is read.</summary>
public sealed record ImportGroup(string Folder, IReadOnlyList<string> AudioFiles);
