namespace OpenMyo.Yoto.Auth;

/// <summary>Supplies bearer tokens to <see cref="YotoAuthHandler"/>.</summary>
internal interface IAccessTokenSource
{
    /// <summary>Returns a token valid for at least a minute, refreshing (single-flight) when needed.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);

    /// <summary>Forces a refresh after Yoto rejected <paramref name="rejectedToken"/>; reuses a newer token if another caller already refreshed.</summary>
    Task<string> RefreshAccessTokenAsync(string rejectedToken, CancellationToken cancellationToken);
}
