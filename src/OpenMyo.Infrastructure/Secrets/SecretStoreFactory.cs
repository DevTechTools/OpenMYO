using OpenMyo.Core.Abstractions;

namespace OpenMyo.Infrastructure.Secrets;

/// <summary>Chooses the strongest secret store the current operating system offers.</summary>
public static class SecretStoreFactory
{
    public static ISecretStore Create(IAppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (OperatingSystem.IsMacOS())
        {
            return new MacKeychainSecretStore();
        }

        if (OperatingSystem.IsWindows())
        {
            return new WindowsDpapiSecretStore(paths);
        }

        return new FileSecretStore(paths);
    }
}
