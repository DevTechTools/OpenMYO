using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Platform;

namespace OpenMyo.App.Services;

/// <summary>
/// Sets the macOS Dock icon from the app logo. OpenMyo ships as a plain executable (no .app bundle with an .icns),
/// and Avalonia does not use <c>Window.Icon</c> for the Dock, so without this the Dock shows the generic "exec" icon.
/// </summary>
[SupportedOSPlatform("macos")]
internal static partial class MacDockIcon
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private static readonly Uri LogoUri = new("avares://OpenMyo/Assets/openmyo-dock.png");

    /// <summary>Must run on the UI thread after Avalonia has started NSApplication.</summary>
    public static void Apply()
    {
        byte[] png;
        using (var stream = AssetLoader.Open(LogoUri))
        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            png = buffer.ToArray();
        }

        // Owned (+1) objects, created once and kept for the life of the process.
        var data = Send(Send(Class("NSData"), Selector("alloc")), Selector("initWithBytes:length:"), png, (nuint)png.Length);
        var image = Send(Send(Class("NSImage"), Selector("alloc")), Selector("initWithData:"), data);
        if (image == IntPtr.Zero)
        {
            return;
        }

        var app = Send(Class("NSApplication"), Selector("sharedApplication"));
        Send(app, Selector("setApplicationIconImage:"), image);
    }

    private static IntPtr Class(string name) => GetClass(name);

    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr GetClass(string name);

    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr Selector(string name);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr Send(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr Send(IntPtr receiver, IntPtr selector, byte[] bytes, nuint length);
}
