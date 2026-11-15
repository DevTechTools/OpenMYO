namespace OpenMyo.Core.Playlists;

/// <summary>Lightweight listing entry returned before a playlist's chapters are loaded.</summary>
public sealed record PlaylistSummary(string CardId, string Title, DateTimeOffset? UpdatedAt, Uri? CoverImageUrl);
