using System.Net;
using System.Text;

namespace OpenMyo.Yoto.Tests.Support;

/// <summary>Scripted handler: routes by "METHOD path" and records every request.</summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<Func<HttpResponseMessage>>> _routes = new(StringComparer.Ordinal);

    public List<string> Requests { get; } = [];

    public List<HttpRequestMessage> Sent { get; } = [];

    public StubHttpHandler On(string route, string json, HttpStatusCode status = HttpStatusCode.OK) =>
        On(route, () => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public StubHttpHandler On(string route, Func<HttpResponseMessage> respond)
    {
        if (!_routes.TryGetValue(route, out var queue))
        {
            _routes[route] = queue = new Queue<Func<HttpResponseMessage>>();
        }

        queue.Enqueue(respond);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var route = $"{request.Method} {request.RequestUri!.PathAndQuery}";
        Requests.Add(route);
        Sent.Add(request);
        if (!_routes.TryGetValue(route, out var queue) || queue.Count == 0)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        // The last scripted response repeats so polling tests need not script every call.
        var respond = queue.Count > 1 ? queue.Dequeue() : queue.Peek();
        return Task.FromResult(respond());
    }
}
