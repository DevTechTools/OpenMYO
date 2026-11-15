using System.Net;
using System.Net.Http.Headers;
using OpenMyo.Yoto.Http;

namespace OpenMyo.Yoto.Auth;

/// <summary>Adds the bearer token; on 401 refreshes once and retries once.</summary>
internal sealed class YotoAuthHandler(IAccessTokenSource tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || !RequestContent.CanResend(request))
        {
            return response;
        }

        response.Dispose();
        token = await tokens.RefreshAccessTokenAsync(token, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
