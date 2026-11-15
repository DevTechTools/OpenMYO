namespace OpenMyo.App.Services;

/// <summary>Native open dialogs. Returns local paths; empty when the user cancels.</summary>
public interface IFilePickerService
{
    Task<IReadOnlyList<string>> PickFoldersAsync();

    Task<IReadOnlyList<string>> PickAudioFilesAsync();

    Task<string?> PickImageAsync();
}
