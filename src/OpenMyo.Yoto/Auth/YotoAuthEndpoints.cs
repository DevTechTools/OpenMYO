namespace OpenMyo.Yoto.Auth;

/// <summary>Fixed OAuth parameters for Yoto's public PKCE client.</summary>
internal static class YotoAuthEndpoints
{
    public const string Audience = "https://api.yotoplay.com";
    /// <summary>
    /// Every scope the app uses, listed explicitly: Yoto checks each one by name even when another scope
    /// documents it as "included" (e.g. listing cards needs <c>user:content:view</c> and <c>family:library:view</c>).
    /// Changing this forces one fresh sign-in (see <c>YotoSession.TryRestoreAsync</c>).
    /// </summary>
    public const string Scope = "user:content:view user:content:manage user:icons:manage family:library:view family:devices:view family:devices:control offline_access";
    public const string RedirectUri = "http://127.0.0.1:8787/callback";
    public const string ListenerPrefix = "http://127.0.0.1:8787/callback/";
    public const string TokenPath = "oauth/token";

    public static readonly Uri AuthorizeUrl = new("https://login.yotoplay.com/authorize");

    /// <summary>Builds the browser URL that starts the authorization code flow.</summary>
    public static Uri BuildAuthorizeUri(string clientId, string codeChallenge, string state)
    {
        var parameters = new (string Name, string Value)[]
        {
            ("audience", Audience),
            ("scope", Scope),
            ("response_type", "code"),
            ("client_id", clientId),
            ("code_challenge", codeChallenge),
            ("code_challenge_method", "S256"),
            ("redirect_uri", RedirectUri),
            ("state", state),
        };
        var query = string.Join('&', parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));
        return new Uri($"{AuthorizeUrl.AbsoluteUri}?{query}");
    }
}
