using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.Editor.Commands;
using OpenMyo.App.Screens.Editor;
using OpenMyo.App.Screens.IconPicker;
using OpenMyo.App.Screens.Import;
using OpenMyo.App.Screens.Import.Feed;
using OpenMyo.App.Screens.Library;
using OpenMyo.App.Screens.Player;
using OpenMyo.App.Screens.Settings;
using OpenMyo.App.Screens.Shell;
using OpenMyo.App.Services;
using OpenMyo.Claude;
using OpenMyo.Core;
using OpenMyo.Infrastructure;
using OpenMyo.Mcp;
using OpenMyo.Yoto;

namespace OpenMyo.App.Composition;

/// <summary>The composition root: the only place that knows every concrete type.</summary>
internal static class ServiceRegistration
{
    private const string UserAgent = "OpenMyo/1.0";

    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
        services.AddHttpClient();

        // Yoto's media CDN rejects requests without a User-Agent (403), and HttpClient sends none by default.
        services.ConfigureHttpClientDefaults(builder => builder.ConfigureHttpClient(client =>
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent)));

        services.AddInfrastructure().AddYoto().AddClaude().AddCore().AddMcpTools();

        services.AddSingleton<WindowProvider>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        services.AddSingleton<RemoteImageCache>();
        services.AddSingleton<IconBitmapLoader>();
        services.AddSingleton<ClaudeDesktopHandoff>();
        services.AddSingleton<EditorIconServices>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LibraryViewModel>();
        services.AddSingleton<PlaylistEditorViewModel>();
        services.AddSingleton<ImportViewModel>();
        services.AddSingleton<FeedImportViewModel>();
        services.AddSingleton<NowPlayingViewModel>();
        services.AddSingleton<RemotePlaybackViewModel>();
        services.AddSingleton<OutputPickerViewModel>();
        services.AddSingleton<LivePlayersViewModel>();
        services.AddSingleton<PlayerBarViewModel>();
        services.AddSingleton<AiKeyViewModel>();
        services.AddSingleton<ClaudeDesktopViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<IconPickerViewModel>();
        services.AddTransient<CommunityResultsViewModel>();
        services.AddSingleton<BulkActionsViewModel>();
        services.AddSingleton<ScreenLinks>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
