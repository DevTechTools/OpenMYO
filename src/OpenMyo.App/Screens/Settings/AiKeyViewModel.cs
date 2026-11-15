using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.App.Screens.Settings;

/// <summary>
/// The Anthropic API key used by "Search with AI". Shared by Settings and every icon picker, so the key can
/// be added right where AI is first wanted. There is no global AI switch: AI runs only on an explicit click.
/// </summary>
public sealed partial class AiKeyViewModel(ISecretStore secrets) : ObservableObject
{
    /// <summary>Write-only input; the stored key is never read back into the UI.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _input = string.Empty;

    [ObservableProperty]
    private bool _hasKey;

    /// <summary>Raised after a key is saved, so a waiting AI search can continue.</summary>
    public event EventHandler? Saved;

    public async Task LoadAsync(CancellationToken cancellationToken) =>
        HasKey = !string.IsNullOrEmpty(await secrets.GetAsync(SecretNames.AnthropicApiKey, cancellationToken));

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await secrets.SetAsync(SecretNames.AnthropicApiKey, Input.Trim(), CancellationToken.None);
        (Input, HasKey) = (string.Empty, true);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        await secrets.DeleteAsync(SecretNames.AnthropicApiKey, CancellationToken.None);
        HasKey = false;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Input);
}
