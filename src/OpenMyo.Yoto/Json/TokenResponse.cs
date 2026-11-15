using System.Text.Json.Serialization;

namespace OpenMyo.Yoto.Json;

/// <summary>Response of the OAuth token endpoint. Tokens are never logged.</summary>
internal sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")]
    public double ExpiresIn { get; init; }
}
