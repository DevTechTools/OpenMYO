using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.ClaudeDesktop;

namespace OpenMyo.App.Screens.Settings;

/// <summary>
/// Adds OpenMyo to Claude Desktop as a connector, so the user can ask Claude (on their own subscription) to
/// pick icons for their playlists. Claude Desktop launches this same app with <see cref="McpArgument"/>.
/// </summary>
public sealed partial class ClaudeDesktopViewModel(IClaudeDesktopConfig config) : ObservableObject
{
    public const string McpArgument = "--mcp";
    private const string ServerName = ConnectorConfigJson.OpenMyoServer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isConnected;

    [ObservableProperty]
    private string? _error;

    public bool IsInstalled => config.IsClaudeDesktopInstalled;

    public string StatusText => !IsInstalled ? "Claude Desktop isn't installed on this computer."
        : IsConnected ? "Connected. Open Claude Desktop; OpenMyo appears in its connectors."
        : "Not connected.";

    public async Task LoadAsync(CancellationToken cancellationToken) =>
        IsConnected = IsInstalled && await config.HasServerAsync(ServerName, cancellationToken);

    [RelayCommand]
    private Task ConnectAsync() => ChangeConfigWhenClosedAsync(async token =>
    {
        var (command, args) = LaunchCommand();
        await config.AddServerAsync(ServerName, command, args, token);
    });

    [RelayCommand]
    private Task DisconnectAsync() => ChangeConfigWhenClosedAsync(token => config.RemoveServerAsync(ServerName, token));

    private async Task ChangeConfigWhenClosedAsync(Func<CancellationToken, Task> change)
    {
        if (config.IsClaudeDesktopRunning)
        {
            Error = OperatingSystem.IsMacOS()
                ? "Quit Claude Desktop first (Claude › Quit, or ⌘Q), then try again. It overwrites this setting while it's open."
                : "Quit Claude Desktop first (right-click its tray icon › Quit), then try again. It overwrites this setting while it's open.";
            return;
        }

        try
        {
            Error = null;
            await change(CancellationToken.None);
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Error = "Couldn't update Claude Desktop's settings: " + exception.Message;
        }
    }

    // A published app runs as its own executable; under `dotnet run` the host is dotnet plus the app's dll.
    private static (string Command, string[] Args) LaunchCommand()
    {
        var process = Environment.ProcessPath ?? throw new InvalidOperationException("Can't find OpenMyo's executable.");
        var isDotnetHost = Path.GetFileNameWithoutExtension(process).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        return isDotnetHost
            ? (process, [Path.Combine(AppContext.BaseDirectory, typeof(ClaudeDesktopViewModel).Assembly.GetName().Name + ".dll"), McpArgument])
            : (process, [McpArgument]);
    }
}
