using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using OpenMyo.App;
using OpenMyo.App.Composition;
using OpenMyo.App.Screens.Editor;
using OpenMyo.App.Screens.Library;
using OpenMyo.App.Services;
using OpenMyo.Core.Playlists;
using OpenMyo.Snapshots;

// Usage: dotnet run --project tools/OpenMyo.Snapshots -- <output folder> [card json]
// Renders the playlist editor in a few states with the app's real styles and saves PNGs.
var output = args.Length > 0 ? args[0] : "snapshots";
Directory.CreateDirectory(output);

AppBuilder.Configure<App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

using var services = ServiceRegistration.Build();
var playlist = SamplePlaylist.Load(args.Length > 1 ? args[1] : null);
var editor = services.GetRequiredService<PlaylistEditorViewModel>();
var row = new PlaylistRowViewModel(new PlaylistSummary(playlist.CardId ?? "sample", playlist.Title, null, playlist.CoverImageUrl), services.GetRequiredService<IconBitmapLoader>());
row.UpdateFrom(playlist);
await editor.OpenAsync(row);

var window = new Window { Width = 1280, Height = 860, Content = new PlaylistEditorView { DataContext = editor } };
if (Application.Current?.TryGetResource("ContentBrush", Avalonia.Styling.ThemeVariant.Dark, out var background) == true)
{
    window.Background = background as Avalonia.Media.IBrush;
}

window.Show();
Capture(window, Path.Combine(output, "editor.png"));

editor.Cleanup.OpenCommand.Execute(null);
Capture(window, Path.Combine(output, "editor-tidy.png"));
editor.Cleanup.CloseCommand.Execute(null);

editor.Selection.Model.SelectRange(0, 3);
Capture(window, Path.Combine(output, "editor-multiselect.png"));

Console.WriteLine($"Saved snapshots to {Path.GetFullPath(output)}");

// Lets async image loads and layout settle before the frame is captured.
static void Capture(Window window, string path)
{
    for (var i = 0; i < 40; i++)
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(50);
    }

    window.CaptureRenderedFrame()?.Save(path);
}
