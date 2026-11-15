using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.Core.ClaudeDesktop;

namespace OpenMyo.App.Screens.Editor.Commands;

/// <summary>
/// The editor's whole-playlist icon actions: match missing icons, re-pick every icon (each optionally with AI)
/// and number icons by track position. Results show as unsaved edits.
/// </summary>
public sealed partial class ChapterIconCommands(
    EditorIconServices services,
    IconPickerViewModel picker,
    ChapterSelection selection,
    TrackSuggestionsViewModel suggestions,
    IChapterEditor editor)
{
    [RelayCommand]
    private Task MatchMissingIconsAsync() => MatchIconsAsync(replaceExisting: false, useAi: false);

    [RelayCommand]
    private Task RematchAllIconsAsync() => MatchIconsAsync(replaceExisting: true, useAi: false);

    /// <summary>Asks Claude only about tracks the free matcher is unsure of; see <see cref="MatchIconsWithAiAsync"/>.</summary>
    [RelayCommand]
    private Task MatchMissingIconsWithAiAsync() => MatchIconsWithAiAsync(replaceExisting: false);

    /// <summary>Re-picks every icon, asking Claude about the titles the free matcher is unsure of.</summary>
    [RelayCommand]
    private Task RematchAllIconsWithAiAsync() => MatchIconsWithAiAsync(replaceExisting: true);

    /// <summary>Shows each track's position as its icon: the selected tracks, or every track when one or none is selected.</summary>
    [RelayCommand]
    private Task NumberIconsAsync() => editor.CurrentPlaylist is not { } playlist
        ? Task.CompletedTask
        : editor.RunAsync("Numbering icons", async token =>
        {
            var indexes = selection.HasMany
                ? selection.Indexes
                : Enumerable.Range(0, playlist.Chapters.Count).ToList();
            var result = await services.Assigner.AssignNumberIconsAsync(playlist, indexes, token);
            editor.ApplyChapters(result.Playlist.Chapters);
            suggestions.Notice = result.MissingNumbers.Count == 0
                ? null
                : $"Number icons go up to 999; track {string.Join(", ", result.MissingNumbers)} kept their icons.";
        });

    /// <summary>
    /// With an API key, Claude runs here. Without one, a connected Claude Desktop gets the request on the user's
    /// own plan; otherwise the picker asks for a key.
    /// </summary>
    private async Task MatchIconsWithAiAsync(bool replaceExisting)
    {
        if (services.AiKey.HasKey)
        {
            await MatchIconsAsync(replaceExisting, useAi: true);
            return;
        }

        if (editor.CurrentPlaylist is not { } playlist || !await services.ClaudeDesktop.IsConnectedAsync(CancellationToken.None))
        {
            picker.CanUseAiOrAskForKey();
            return;
        }

        var prompt = ClaudeDesktopPrompts.PickIcons(playlist.Title, playlist.CardId, replaceExisting);
        var opened = await services.ClaudeDesktop.SendAsync(prompt);
        var where = opened
            ? "Opened Claude Desktop with the request ready; press Return there to send it"
            : "Copied a request for Claude Desktop; open it and paste the request into a new chat";
        suggestions.Notice = $"{where}. Claude saves straight to Yoto, so reopen this playlist when it's done.";
    }

    private Task MatchIconsAsync(bool replaceExisting, bool useAi) => editor.CurrentPlaylist is not { } playlist
        ? Task.CompletedTask
        : editor.RunAsync("Matching icons", async token =>
        {
            var matched = await services.Assigner.MatchIconsAsync(playlist, replaceExisting, useAi, token);
            editor.ApplyChapters(matched.Playlist.Chapters);
            suggestions.Notice = matched.CommunityIconCount == 0 ? null : $"Added {matched.CommunityIconCount} from yotoicons.com.";
        });
}
