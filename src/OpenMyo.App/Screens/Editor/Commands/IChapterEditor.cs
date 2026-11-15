using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Editor.Commands;

/// <summary>
/// What the playlist editor offers the command groups it owns (see <see cref="ChapterIconCommands"/>): run work
/// with the editor's progress and error handling, read the playlist as currently edited, and show changed
/// chapters as unsaved edits. Implemented by <see cref="PlaylistEditorViewModel"/>.
/// </summary>
public interface IChapterEditor
{
    /// <summary>The playlist with the user's unsaved edits applied; null when none is open.</summary>
    Playlist? CurrentPlaylist { get; }

    Task RunAsync(string status, Func<CancellationToken, Task> operation);

    void ApplyChapters(IReadOnlyList<Chapter> chapters);
}
