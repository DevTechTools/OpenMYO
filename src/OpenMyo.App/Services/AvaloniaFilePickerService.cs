using Avalonia.Platform.Storage;
using OpenMyo.Core.Import;

namespace OpenMyo.App.Services;

public sealed class AvaloniaFilePickerService(WindowProvider windowProvider) : IFilePickerService
{
    private static readonly FilePickerFileType AudioFiles = new("Audio")
    {
        Patterns = MediaTypes.AudioExtensions.Select(extension => "*" + extension).ToList(),
    };

    private static readonly FilePickerFileType ImageFiles = new("Images")
    {
        Patterns = MediaTypes.ImageExtensions.Select(extension => "*" + extension).ToList(),
    };

    private IStorageProvider Storage => windowProvider.TopLevel.StorageProvider;

    public async Task<IReadOnlyList<string>> PickFoldersAsync()
    {
        var folders = await Storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose folders (each becomes a playlist)",
            AllowMultiple = true,
        });
        return LocalPaths(folders);
    }

    public async Task<IReadOnlyList<string>> PickAudioFilesAsync()
    {
        var files = await Storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose audio files",
            AllowMultiple = true,
            FileTypeFilter = [AudioFiles],
        });
        return LocalPaths(files);
    }

    public async Task<string?> PickImageAsync()
    {
        var files = await Storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a cover image",
            FileTypeFilter = [ImageFiles],
        });
        return LocalPaths(files).FirstOrDefault();
    }

    private static List<string> LocalPaths(IEnumerable<IStorageItem> items) =>
        items.Select(item => item.TryGetLocalPath()).OfType<string>().ToList();
}
