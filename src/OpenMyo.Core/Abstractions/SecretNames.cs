namespace OpenMyo.Core.Abstractions;

/// <summary>Well-known secret names so every layer agrees on keys.</summary>
public static class SecretNames
{
    public const string YotoRefreshToken = "yoto-refresh-token";

    /// <summary>The OAuth scope the stored refresh token was granted for; a mismatch forces a new sign-in.</summary>
    public const string YotoTokenScope = "yoto-token-scope";
    public const string AnthropicApiKey = "anthropic-api-key";
}
