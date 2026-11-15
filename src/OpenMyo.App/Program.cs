using Avalonia;
using OpenMyo.App.Composition;
using OpenMyo.App.Screens.Settings;

namespace OpenMyo.App;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains(ClaudeDesktopViewModel.McpArgument, StringComparer.Ordinal))
        {
            // Process entry point with no UI or synchronization context: waiting here cannot deadlock.
            return McpMode.RunAsync().GetAwaiter().GetResult();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>Also used by the visual designer; keep free of side effects.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions { ShowInDock = true })
            .LogToTrace();
}
