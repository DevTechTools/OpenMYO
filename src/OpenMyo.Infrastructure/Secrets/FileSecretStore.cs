using System.Text;
using OpenMyo.Core.Abstractions;
using OpenMyo.Infrastructure.Paths;

namespace OpenMyo.Infrastructure.Secrets;

/// <summary>
/// Development-only fallback for operating systems without a supported secure store (Linux).
/// Secrets are stored UNENCRYPTED in owner-only files (mode 600), so this is less secure than the
/// macOS Keychain or Windows DPAPI stores. Never ship it as the production store.
/// </summary>
public sealed class FileSecretStore(IAppPaths paths) : ISecretStore
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken)
    {
        var path = SecretFilePath(name);
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    public Task SetAsync(string name, string value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        var path = SecretFilePath(name);
        var directory = Path.GetDirectoryName(path)!;
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, OwnerOnly);
        }

        return AtomicFile.WriteAsync(path, Encoding.UTF8.GetBytes(value), cancellationToken);
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        File.Delete(SecretFilePath(name));
        return Task.CompletedTask;
    }

    private string SecretFilePath(string name) =>
        Path.Combine(paths.DataDirectory, "secrets", SecretName.Validate(name) + ".secret");
}
