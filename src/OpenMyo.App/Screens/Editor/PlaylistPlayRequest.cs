using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Editor;

/// <summary>Play <see cref="Playlist"/> from the row with <see cref="ChapterKey"/>.</summary>
public sealed record PlaylistPlayRequest(Playlist Playlist, string ChapterKey);
