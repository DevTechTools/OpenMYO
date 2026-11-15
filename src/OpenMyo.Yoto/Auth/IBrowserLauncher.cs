using System.Diagnostics;

namespace OpenMyo.Yoto.Auth;

/// <summary>Seam over opening the system browser so the sign-in flow can be tested.</summary>
internal interface IBrowserLauncher
{
    void Open(Uri url);
}

internal sealed class SystemBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        using var process = Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }
}
