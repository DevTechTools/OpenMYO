namespace OpenMyo.Core.Abstractions;

/// <summary>
/// OS-backed storage for secrets (OAuth refresh tokens, API keys).
/// macOS: Keychain. Windows: DPAPI (current user).
/// </summary>
public interface ISecretStore
{
    Task<string?> GetAsync(string name, CancellationToken cancellationToken);

    Task SetAsync(string name, string value, CancellationToken cancellationToken);

    Task DeleteAsync(string name, CancellationToken cancellationToken);
}
