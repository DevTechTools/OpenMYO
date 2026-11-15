using System.Net;
using System.Text;

namespace OpenMyo.Yoto.Auth;

/// <summary>Runs the browser half of the PKCE flow: opens Yoto login and catches the loopback redirect.</summary>
internal sealed class LoopbackAuthorizer(IBrowserLauncher browser)
{
    private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(5);

    private const string PageStart =
        "<!doctype html><html><head><meta charset=\"utf-8\"><title>OpenMyo</title></head>" +
        "<body style=\"font-family:system-ui,sans-serif;text-align:center;margin-top:20vh\">";

    private const string PageEnd = "</body></html>";

    private const string SuccessBody = "<h2>You're signed in</h2><p>You can close this window and return to OpenMyo.</p>";
    private const string FailureBody = "<h2>Sign-in did not complete</h2><p>You can close this window and try again in OpenMyo.</p>";

    /// <returns>The authorization code.</returns>
    public async Task<string> AuthorizeAsync(Uri authorizeUrl, string expectedState, CancellationToken cancellationToken)
    {
        using var listener = StartListener();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SignInTimeout);
        using var registration = timeout.Token.Register(listener.Close);
        browser.Open(authorizeUrl);

        while (true)
        {
            var context = await NextRequestAsync(listener, timeout.Token, cancellationToken);
            var callback = AuthorizationCallback.Parse(context.Request.Url, expectedState);
            if (callback is null)
            {
                await RespondAsync(context, HttpStatusCode.NotFound, FailureBody);
                continue;
            }

            var succeeded = callback.Code is not null;
            await RespondAsync(context, succeeded ? HttpStatusCode.OK : HttpStatusCode.BadRequest, succeeded ? SuccessBody : FailureBody);
            return callback.Code ?? throw new InvalidOperationException($"Yoto sign-in was not completed ({callback.Error}).");
        }
    }

    private static HttpListener StartListener()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(YotoAuthEndpoints.ListenerPrefix);
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            listener.Close();
            throw new InvalidOperationException("Could not listen on 127.0.0.1:8787 for the Yoto sign-in redirect. Close other apps using that port and try again.", ex);
        }

        return listener;
    }

    private static async Task<HttpListenerContext> NextRequestAsync(HttpListener listener, CancellationToken timeoutToken, CancellationToken userToken)
    {
        try
        {
            return await listener.GetContextAsync();
        }
        catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
        {
            userToken.ThrowIfCancellationRequested();
            throw new TimeoutException("Yoto sign-in timed out. Try again.", ex);
        }
    }

    private static async Task RespondAsync(HttpListenerContext context, HttpStatusCode status, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(PageStart + body + PageEnd);
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }
}
