using OpenMyo.Yoto.Http;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Auth;

/// <summary>Calls Yoto's OAuth token endpoint.</summary>
internal sealed class YotoTokenClient(IHttpClientFactory httpClientFactory)
{
    public Task<TokenResponse> ExchangeCodeAsync(string clientId, string codeVerifier, string code, CancellationToken cancellationToken) =>
        PostAsync(
            [
                new("grant_type", "authorization_code"),
                new("client_id", clientId),
                new("code_verifier", codeVerifier),
                new("code", code),
                new("redirect_uri", YotoAuthEndpoints.RedirectUri),
            ],
            cancellationToken);

    /// <summary>Yoto refresh tokens are single-use: callers must store the refresh token in the response.</summary>
    public Task<TokenResponse> RefreshAsync(string clientId, string refreshToken, CancellationToken cancellationToken) =>
        PostAsync(
            [
                new("grant_type", "refresh_token"),
                new("client_id", clientId),
                new("refresh_token", refreshToken),
            ],
            cancellationToken);

    private async Task<TokenResponse> PostAsync(KeyValuePair<string, string>[] form, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Auth);
        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(YotoAuthEndpoints.TokenPath, content, cancellationToken);
        return await YotoResponse.ReadAsync(response, YotoJsonContext.Default.TokenResponse, cancellationToken);
    }
}
