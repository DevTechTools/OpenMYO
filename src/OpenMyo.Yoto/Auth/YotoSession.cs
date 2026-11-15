using System.Net;
using OpenMyo.Core.Abstractions;
using OpenMyo.Yoto.Http;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Auth;

/// <summary>
/// Holds the access token in memory and the refresh token in <see cref="ISecretStore"/>.
/// All refreshes are serialised, within this process by one gate and across OpenMyo processes (the app and
/// the Claude Desktop connector) by <see cref="RefreshLock"/>, because Yoto refresh tokens are single-use.
/// </summary>
internal sealed class YotoSession(
    YotoTokenClient tokenClient,
    LoopbackAuthorizer authorizer,
    ISecretStore secrets,
    ISettingsStore settings,
    RefreshLock refreshLock) : IYotoSession, IAccessTokenSource, IDisposable
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);
    private const double DefaultLifetimeSeconds = 3600;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;
    private volatile bool _isSignedIn;

    public event EventHandler? SignInStateChanged;

    public bool IsSignedIn => _isSignedIn;

    public async Task<bool> TryRestoreAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(await secrets.GetAsync(SecretNames.YotoRefreshToken, cancellationToken)))
        {
            return false;
        }

        // A token granted for an older scope list cannot be widened by refreshing; sign in again instead.
        var grantedScope = await secrets.GetAsync(SecretNames.YotoTokenScope, cancellationToken);
        if (!string.Equals(grantedScope, YotoAuthEndpoints.Scope, StringComparison.Ordinal))
        {
            await secrets.DeleteAsync(SecretNames.YotoRefreshToken, cancellationToken);
            return false;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await RefreshUnderLockAsync(cancellationToken);
            return true;
        }
        catch (YotoApiException ex) when (IsRejection(ex))
        {
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SignInAsync(CancellationToken cancellationToken)
    {
        var clientId = await RequireClientIdAsync(cancellationToken);
        var verifier = Pkce.CreateVerifier();
        var state = Pkce.CreateState();
        var url = YotoAuthEndpoints.BuildAuthorizeUri(clientId, Pkce.CreateChallenge(verifier), state);

        var code = await authorizer.AuthorizeAsync(url, state, cancellationToken);
        var response = await tokenClient.ExchangeCodeAsync(clientId, verifier, code, cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await StoreTokensAsync(response, cancellationToken);
            await secrets.SetAsync(SecretNames.YotoTokenScope, YotoAuthEndpoints.Scope, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await ForgetTokensAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (HasFreshToken())
        {
            return _accessToken!; // HasFreshToken guarantees non-null.
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (HasFreshToken())
            {
                return _accessToken!;
            }

            await RefreshUnderLockAsync(cancellationToken);
            return _accessToken!; // RefreshUnderLockAsync sets it or throws.
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> RefreshAccessTokenAsync(string rejectedToken, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is { } current && !string.Equals(current, rejectedToken, StringComparison.Ordinal))
            {
                return current;
            }

            await RefreshUnderLockAsync(cancellationToken);
            return _accessToken!; // RefreshUnderLockAsync sets it or throws.
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private static bool IsRejection(YotoApiException ex) =>
        ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    private bool HasFreshToken() =>
        _accessToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshMargin;

    private async Task RefreshUnderLockAsync(CancellationToken cancellationToken)
    {
        // Read the token inside the lock: another process may have just replaced it.
        using var processLock = await refreshLock.AcquireAsync(cancellationToken);
        var refreshToken = await secrets.GetAsync(SecretNames.YotoRefreshToken, cancellationToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            await ForgetTokensAsync(cancellationToken);
            throw new InvalidOperationException("Not signed in to Yoto. Sign in first.");
        }

        var clientId = await RequireClientIdAsync(cancellationToken);
        try
        {
            var response = await tokenClient.RefreshAsync(clientId, refreshToken, cancellationToken);
            await StoreTokensAsync(response, cancellationToken);
        }
        catch (YotoApiException ex) when (IsRejection(ex))
        {
            await ForgetTokensAsync(cancellationToken);
            throw;
        }
    }

    private async Task StoreTokensAsync(TokenResponse response, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new InvalidOperationException("Yoto did not return an access token.");
        }

        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            await secrets.SetAsync(SecretNames.YotoRefreshToken, response.RefreshToken, cancellationToken);
        }

        var lifetime = response.ExpiresIn > 0 ? response.ExpiresIn : DefaultLifetimeSeconds;
        _accessToken = response.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(lifetime);
        SetSignedIn(true);
    }

    private async Task ForgetTokensAsync(CancellationToken cancellationToken)
    {
        _accessToken = null;
        await secrets.DeleteAsync(SecretNames.YotoRefreshToken, cancellationToken);
        SetSignedIn(false);
    }

    private void SetSignedIn(bool value)
    {
        if (_isSignedIn == value)
        {
            return;
        }

        _isSignedIn = value;
        SignInStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task<string> RequireClientIdAsync(CancellationToken cancellationToken)
    {
        var loaded = await settings.LoadAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(loaded.YotoClientId)
            ? throw new InvalidOperationException("Set your Yoto client id in Settings before signing in.")
            : loaded.YotoClientId.Trim();
    }
}
