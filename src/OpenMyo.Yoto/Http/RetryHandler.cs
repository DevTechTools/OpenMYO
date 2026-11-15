using System.Net;

namespace OpenMyo.Yoto.Http;

/// <summary>Retries 429 and 503 responses, honouring <c>Retry-After</c>, up to three attempts.</summary>
internal sealed class RetryHandler : DelegatingHandler
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!IsRetryable(response) || attempt >= MaxAttempts || !RequestContent.CanResend(request))
            {
                return response;
            }

            var delay = DelayFor(response, attempt);
            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static bool IsRetryable(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;

    private static TimeSpan DelayFor(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        var delay = retryAfter?.Delta
            ?? (retryAfter?.Date - DateTimeOffset.UtcNow)
            ?? BaseDelay * attempt;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay > MaxDelay ? MaxDelay : delay;
    }
}
