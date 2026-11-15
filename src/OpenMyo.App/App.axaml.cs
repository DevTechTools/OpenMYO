using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Composition;
using OpenMyo.App.Screens.Shell;
using OpenMyo.App.Services;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.App;

public sealed partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = ServiceRegistration.Build();
            _services.GetRequiredService<ScreenLinks>().Connect();
            var window = new MainWindow { DataContext = _services.GetRequiredService<MainViewModel>() };
            _services.GetRequiredService<WindowProvider>().Attach(window);
            desktop.MainWindow = window;
            if (OperatingSystem.IsMacOS())
            {
                MacDockIcon.Apply();
            }

            desktop.ShutdownRequested += OnShutdownRequested;
            WarmUpCommunityIconsAsync(_services.GetRequiredService<CommunityCatalogProvider>())
                .Forget(_services.GetRequiredService<ILogger<App>>(), "reading the yotoicons.com catalog");
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Some services (the player connection) can only be disposed asynchronously, and a synchronous
    /// <c>Dispose</c> of the container throws. So the first shutdown request is held back while they close,
    /// then shutdown is requested again and goes through.
    /// </summary>
    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_services is not { } services || sender is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        e.Cancel = true;
        _services = null;
        var logger = services.GetRequiredService<ILogger<App>>();
        try
        {
            await services.DisposeAsync();
        }
        catch (Exception exception)
        {
            // UI boundary: the app is closing either way, so a failed clean-up is only logged.
            logger.LogError(exception, "Disposing services on shutdown failed");
        }
        finally
        {
            desktop.Shutdown();
        }
    }

    /// <summary>Reads the yotoicons.com catalog in the background (weekly) so community search is instant.</summary>
    private static async Task WarmUpCommunityIconsAsync(CommunityCatalogProvider catalog)
    {
        try
        {
            await catalog.WarmUpAsync(null, CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            // Offline: the picker retries on its first community search and says so there.
        }
    }
}
