using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.Infrastructure.Secrets;

/// <summary>Stores secrets as generic passwords in the user's login keychain (service "OpenMyo").</summary>
[SupportedOSPlatform("macos")]
public sealed class MacKeychainSecretStore : ISecretStore
{
    private static readonly byte[] Service = Encoding.UTF8.GetBytes("OpenMyo");

    public Task<string?> GetAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ReadPassword(AccountBytes(name)));
    }

    public Task SetAsync(string name, string value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        WritePassword(AccountBytes(name), Encoding.UTF8.GetBytes(value));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var status = MacKeychainNative.SecKeychainFindGenericPassword(
            IntPtr.Zero, (uint)Service.Length, Service, (uint)AccountBytes(name).Length, AccountBytes(name), out _, out var data, out var item);
        if (status == MacKeychainNative.ItemNotFound)
        {
            return Task.CompletedTask;
        }

        try
        {
            ThrowIfFailed(status, "find");
            ThrowIfFailed(MacKeychainNative.SecKeychainItemDelete(item), "delete");
        }
        finally
        {
            ReleaseItem(item, data);
        }

        return Task.CompletedTask;
    }

    private static byte[] AccountBytes(string name) => Encoding.UTF8.GetBytes(SecretName.Validate(name));

    private static string? ReadPassword(byte[] account)
    {
        var status = MacKeychainNative.SecKeychainFindGenericPassword(
            IntPtr.Zero, (uint)Service.Length, Service, (uint)account.Length, account, out var length, out var data, out var item);
        if (status == MacKeychainNative.ItemNotFound)
        {
            return null;
        }

        try
        {
            ThrowIfFailed(status, "read");
            var bytes = new byte[length];
            Marshal.Copy(data, bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            ReleaseItem(item, data);
        }
    }

    private static void WritePassword(byte[] account, byte[] value)
    {
        var status = MacKeychainNative.SecKeychainFindGenericPassword(
            IntPtr.Zero, (uint)Service.Length, Service, (uint)account.Length, account, out _, out var data, out var item);
        try
        {
            if (status == MacKeychainNative.ItemNotFound)
            {
                ThrowIfFailed(MacKeychainNative.SecKeychainAddGenericPassword(
                    IntPtr.Zero, (uint)Service.Length, Service, (uint)account.Length, account, (uint)value.Length, value, IntPtr.Zero), "add");
                return;
            }

            ThrowIfFailed(status, "find");
            ThrowIfFailed(MacKeychainNative.SecKeychainItemModifyAttributesAndData(item, IntPtr.Zero, (uint)value.Length, value), "update");
        }
        finally
        {
            ReleaseItem(item, data);
        }
    }

    private static void ReleaseItem(IntPtr item, IntPtr data)
    {
        if (data != IntPtr.Zero)
        {
            _ = MacKeychainNative.SecKeychainItemFreeContent(IntPtr.Zero, data);
        }

        if (item != IntPtr.Zero)
        {
            MacKeychainNative.CFRelease(item);
        }
    }

    private static void ThrowIfFailed(int status, string operation)
    {
        if (status != MacKeychainNative.Success)
        {
            throw new InvalidOperationException($"macOS Keychain {operation} failed with OSStatus {status}.");
        }
    }
}
