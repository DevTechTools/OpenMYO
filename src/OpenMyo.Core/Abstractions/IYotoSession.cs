namespace OpenMyo.Core.Abstractions;

/// <summary>The signed-in Yoto account. The app drives sign-in; API clients obtain tokens internally.</summary>
public interface IYotoSession
{
    bool IsSignedIn { get; }

    /// <summary>Raised whenever <see cref="IsSignedIn"/> changes (sign in, sign out, refresh token rejected).</summary>
    event EventHandler? SignInStateChanged;

    /// <summary>
    /// Signs in silently with the stored refresh token. Returns false when there is none or Yoto rejected it;
    /// network failures throw so the app can tell "offline" from "signed out".
    /// </summary>
    Task<bool> TryRestoreAsync(CancellationToken cancellationToken);

    /// <summary>Opens the system browser for Yoto login and waits for the loopback redirect.</summary>
    Task SignInAsync(CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}
