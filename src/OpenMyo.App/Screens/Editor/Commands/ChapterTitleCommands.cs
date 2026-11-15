using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.Core.ClaudeDesktop;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.Editor.Commands;

/// <summary>
/// The editor's AI title action: renames every track with Claude. Results show as unsaved edits, so Revert
/// undoes them.
/// </summary>
public sealed partial class ChapterTitleCommands(
    AiTitleRenamer renamer,
    EditorIconServices services,
    IconPickerViewModel picker,
    TrackSuggestionsViewModel suggestions,
    IChapterEditor editor)
{
    /// <summary>
    /// With an API key, Claude runs here. Without one, a connected Claude Desktop gets the request on the user's
    /// own plan; otherwise the picker asks for a key.
    /// </summary>
    [RelayCommand]
    private async Task RenameAllTitlesWithAiAsync()
    {
        if (editor.CurrentPlaylist is not { } playlist)
        {
            return;
        }

        if (services.AiKey.HasKey)
        {
            await editor.RunAsync("Renaming tracks with AI", async token =>
            {
                var result = await renamer.RenameAllAsync(playlist, token);
                if (result.ChangedCount > 0)
                {
                    editor.ApplyChapters(result.Chapters);
                }

                suggestions.Notice = result.ChangedCount == 0
                    ? "Claude kept every title as it is."
                    : $"Claude renamed {result.ChangedCount} track(s). Review them, then Save, or Revert to undo.";
            });
            return;
        }

        if (!await services.ClaudeDesktop.IsConnectedAsync(CancellationToken.None))
        {
            picker.CanUseAiOrAskForKey();
            return;
        }

        var opened = await services.ClaudeDesktop.SendAsync(ClaudeDesktopPrompts.RenameTracks(playlist.Title, playlist.CardId));
        var where = opened
            ? "Opened Claude Desktop with the request ready; press Return there to send it"
            : "Copied a request for Claude Desktop; open it and paste the request into a new chat";
        suggestions.Notice = $"{where}. Claude saves straight to Yoto, so reopen this playlist when it's done.";
    }
}
