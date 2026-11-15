namespace OpenMyo.Core.Playback;

/// <summary>One thing the player can play: a Yoto track (signed stream URL) or a local file.</summary>
/// <param name="RowKey">Identifies the row the item came from (chapter key or file path), so that row can show it is playing.</param>
public sealed record PlaybackItem(string RowKey, string Title, Uri Source, TimeSpan Duration);
