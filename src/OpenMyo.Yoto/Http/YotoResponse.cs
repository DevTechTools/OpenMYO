using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Http;

/// <summary>Turns HTTP responses into parsed JSON or a <see cref="YotoApiException"/>.</summary>
internal static class YotoResponse
{
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
        where T : class
    {
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken)
            ?? throw new YotoApiException(response.StatusCode, null, "Yoto returned an empty response.");
    }

    public static async Task<JsonObject> ReadObjectAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var node = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken);
        return node as JsonObject
            ?? throw new YotoApiException(response.StatusCode, null, "Yoto returned an unexpected response.");
    }

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw CreateException(response.StatusCode, body);
    }

    /// <summary>
    /// Understands Yoto's <c>{"error":{"code","message"}}</c> and OAuth's <c>{"error","error_description"}</c>.
    /// </summary>
    internal static YotoApiException CreateException(HttpStatusCode status, string body)
    {
        string? code = null;
        string? message = null;
        try
        {
            var root = JsonNode.Parse(body);
            var error = root.GetObject("error");
            code = error is null ? root.GetString("error") : error.GetString("code");
            message = error is null ? root.GetString("error_description") : error.GetString("message");
        }
        catch (JsonException)
        {
            // Non-JSON error bodies (e.g. object storage XML) fall back to the status line.
        }

        var detail = string.IsNullOrWhiteSpace(message) ? code : message;
        var text = string.IsNullOrWhiteSpace(detail)
            ? $"Yoto request failed ({(int)status} {status})."
            : $"Yoto request failed ({(int)status} {status}): {detail}";
        return new YotoApiException(status, code, text);
    }
}
