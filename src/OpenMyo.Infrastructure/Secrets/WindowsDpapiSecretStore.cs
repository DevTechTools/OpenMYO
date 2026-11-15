using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using OpenMyo.Core.Abstractions;
using OpenMyo.Infrastructure.Paths;

namespace OpenMyo.Infrastructure.Secrets;

/// <summary>Encrypts each secret with DPAPI (current user) and stores it as <c>secrets/&lt;name&gt;.bin</c>.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiSecretStore(IAppPaths paths) : ISecretStore
{
    // Fixed, app-specific entropy: stops other DPAPI callers of the same user from decrypting casually.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("OpenMyo.SecretStore.v1");

    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken)
    {
        var path = SecretFilePath(paths, name);
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    public Task SetAsync(string name, string value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        var path = SecretFilePath(paths, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);
        return AtomicFile.WriteAsync(path, protectedBytes, cancellationToken);
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        File.Delete(SecretFilePath(paths, name));
        return Task.CompletedTask;
    }

    internal static string SecretFilePath(IAppPaths paths, string name) =>
        Path.Combine(paths.DataDirectory, "secrets", SecretName.Validate(name) + ".bin");
}
