using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OpenMyo.Infrastructure.Secrets;

/// <summary>Minimal Security.framework / CoreFoundation bindings for generic keychain passwords.</summary>
[SupportedOSPlatform("macos")]
internal static partial class MacKeychainNative
{
    public const int Success = 0;
    public const int ItemNotFound = -25300;

    private const string Security = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [LibraryImport(Security)]
    public static partial int SecKeychainAddGenericPassword(
        IntPtr keychain,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        uint passwordLength,
        byte[] passwordData,
        IntPtr itemRef);

    [LibraryImport(Security)]
    public static partial int SecKeychainFindGenericPassword(
        IntPtr keychainOrArray,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    [LibraryImport(Security)]
    public static partial int SecKeychainItemModifyAttributesAndData(
        IntPtr itemRef,
        IntPtr attributeList,
        uint length,
        byte[] data);

    [LibraryImport(Security)]
    public static partial int SecKeychainItemDelete(IntPtr itemRef);

    [LibraryImport(Security)]
    public static partial int SecKeychainItemFreeContent(IntPtr attributeList, IntPtr data);

    [LibraryImport(CoreFoundation)]
    public static partial void CFRelease(IntPtr reference);
}
