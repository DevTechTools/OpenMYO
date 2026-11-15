using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.App.Screens.Settings;

/// <summary>Account and preferences. Changes apply immediately, like system settings on macOS and Windows.</summary>
public sealed partial class SettingsViewModel(
    ISettingsStore settingsStore, AiKeyViewModel aiKey, ClaudeDesktopViewModel claudeDesktop, IYotoSession session, ILogger<SettingsViewModel> logger)
    : OperationViewModel(logger)
{
    private bool _isLoading;

    [ObservableProperty]
    private string _yotoClientId = string.Empty;

    [ObservableProperty]
    private string _claudeModel = new AppSettings().ClaudeModel;

    [ObservableProperty]
    private int _maxParallelUploads = new AppSettings().MaxParallelUploads;

    [ObservableProperty]
    private int _maxParallelDownloads = new AppSettings().MaxParallelDownloads;

    public AiKeyViewModel AiKey => aiKey;

    public ClaudeDesktopViewModel ClaudeDesktop => claudeDesktop;

    public bool IsSignedIn => session.IsSignedIn;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        _isLoading = true;
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken);
            (YotoClientId, ClaudeModel, MaxParallelUploads, MaxParallelDownloads) =
                (settings.YotoClientId, settings.ClaudeModel, settings.MaxParallelUploads, settings.MaxParallelDownloads);
            await aiKey.LoadAsync(cancellationToken);
            await claudeDesktop.LoadAsync(cancellationToken);
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void RefreshSignInState() => OnPropertyChanged(nameof(IsSignedIn));

    partial void OnYotoClientIdChanged(string value) => SaveInBackground();

    partial void OnClaudeModelChanged(string value) => SaveInBackground();

    partial void OnMaxParallelUploadsChanged(int value) => SaveInBackground();

    partial void OnMaxParallelDownloadsChanged(int value) => SaveInBackground();

    [RelayCommand]
    private Task SignInAsync() => RunAsync("Waiting for Yoto sign-in in your browser", session.SignInAsync);

    [RelayCommand]
    private Task SignOutAsync() => RunAsync("Signing out", session.SignOutAsync);

    private void SaveInBackground()
    {
        if (!_isLoading)
        {
            SaveAsync().Forget(Logger, "saving settings");
        }
    }

    private async Task SaveAsync()
    {
        var current = await settingsStore.LoadAsync(CancellationToken.None);
        var settings = current with
        {
            YotoClientId = YotoClientId.Trim(),
            ClaudeModel = string.IsNullOrWhiteSpace(ClaudeModel) ? new AppSettings().ClaudeModel : ClaudeModel.Trim(),
            MaxParallelUploads = Math.Clamp(MaxParallelUploads, 1, 8),
            MaxParallelDownloads = Math.Clamp(MaxParallelDownloads, 1, 8),
        };
        await settingsStore.SaveAsync(settings, CancellationToken.None);
    }
}
