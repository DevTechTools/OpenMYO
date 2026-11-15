using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using OpenMyo.Core.Feeds;

namespace OpenMyo.App.Screens.Import;

/// <summary>
/// Code-behind only translates drag-and-drop into local paths, and a pasted feed link into opening the feed dialog
/// (view-only plumbing).
/// </summary>
public sealed partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(KeyDownEvent, OnKeyDown);
    }

    private static void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var paths = e.DataTransfer.TryGetFiles()?.Select(item => item.TryGetLocalPath()).OfType<string>().ToList() ?? [];
        if (paths.Count > 0 && DataContext is ImportViewModel viewModel)
        {
            await viewModel.AddPathsAsync(paths);
        }
    }

    // Text boxes keep their own paste; anywhere else on the screen a pasted feed link opens the feed dialog.
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var isPaste = topLevel?.PlatformSettings?.HotkeyConfiguration.Paste.Any(gesture => gesture.Matches(e)) == true;
        if (!isPaste || e.Source is TextBox || topLevel?.Clipboard is not { } clipboard || DataContext is not ImportViewModel viewModel)
        {
            return;
        }

        var text = await clipboard.TryGetTextAsync();
        if (FeedUrl.TryParse(text) is not null)
        {
            viewModel.Feed.Open(text);
        }
    }
}
