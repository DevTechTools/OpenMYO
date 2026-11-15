using System.Web;

namespace OpenMyo.Yoto.Auth;

/// <summary>Outcome of one request to the loopback redirect URI.</summary>
/// <param name="Code">Authorization code when sign-in succeeded.</param>
/// <param name="Error">Why sign-in failed, or null.</param>
internal sealed record AuthorizationCallback(string? Code, string? Error)
{
    private const string CallbackPath = "/callback";

    /// <summary>Parses a redirect request; null when it is not the OAuth callback (e.g. a favicon request).</summary>
    public static AuthorizationCallback? Parse(Uri? url, string expectedState)
    {
        if (url is null || !url.AbsolutePath.TrimEnd('/').Equals(CallbackPath, StringComparison.Ordinal))
        {
            return null;
        }

        var query = HttpUtility.ParseQueryString(url.Query);
        var error = query["error"];
        var code = query["code"];
        if (error is null && code is null)
        {
            return null;
        }

        if (error is not null)
        {
            return new AuthorizationCallback(null, error);
        }

        return string.Equals(query["state"], expectedState, StringComparison.Ordinal)
            ? new AuthorizationCallback(code, null)
            : new AuthorizationCallback(null, "state_mismatch");
    }
}
