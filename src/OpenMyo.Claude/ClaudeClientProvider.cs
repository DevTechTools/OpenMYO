using Anthropic;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.Claude;

/// <summary>
/// Hands out one Anthropic client per stored API key, shared by every Claude feature; a changed key in Settings
/// creates a new client.
/// </summary>
public sealed class ClaudeClientProvider(ISecretStore secrets) : IDisposable
{
    private readonly SemaphoreSlim _clientGate = new(1, 1);
    private AnthropicClient? _client;
    private string? _clientKey;

    public async Task<bool> HasKeyAsync(CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(await secrets.GetAsync(SecretNames.AnthropicApiKey, cancellationToken));

    public async Task<AnthropicClient> GetAsync(CancellationToken cancellationToken)
    {
        var apiKey = await secrets.GetAsync(SecretNames.AnthropicApiKey, cancellationToken)
            ?? throw new InvalidOperationException("Add an Anthropic API key in Settings to use Claude.");

        await _clientGate.WaitAsync(cancellationToken);
        try
        {
            if (_client is null || !string.Equals(_clientKey, apiKey, StringComparison.Ordinal))
            {
                _client = new AnthropicClient { ApiKey = apiKey };
                _clientKey = apiKey;
            }

            return _client;
        }
        finally
        {
            _clientGate.Release();
        }
    }

    public void Dispose() => _clientGate.Dispose();
}
