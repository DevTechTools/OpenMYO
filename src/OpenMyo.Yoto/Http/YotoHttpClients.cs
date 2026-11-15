namespace OpenMyo.Yoto.Http;

/// <summary>Names and base addresses of the HTTP clients registered by <c>AddYoto</c>.</summary>
internal static class YotoHttpClients
{
    /// <summary>api.yotoplay.com with bearer auth, token refresh and retry.</summary>
    public const string Api = "yoto-api";

    /// <summary>login.yotoplay.com; token endpoint, no bearer header.</summary>
    public const string Auth = "yoto-auth";

    /// <summary>Presigned upload URLs; must never carry the bearer token.</summary>
    public const string Upload = "yoto-upload";

    public static readonly Uri ApiBaseAddress = new("https://api.yotoplay.com/");

    public static readonly Uri LoginBaseAddress = new("https://login.yotoplay.com/");
}
