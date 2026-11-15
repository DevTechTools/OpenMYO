using ModelContextProtocol;

namespace OpenMyo.Mcp.Support;

/// <summary>
/// Turns failures Claude can act on into MCP tool errors (<c>isError</c>) with a plain message, so a tool never
/// surfaces a stack trace. Anything unexpected is left to the SDK, which reports a generic error.
/// </summary>
public static class ToolGuard
{
    /// <summary>Shown when the app has no Yoto session; the user must fix it in the app.</summary>
    public const string SignInMessage = "Open OpenMyo and sign in to Yoto first.";

    private const string NotSignedInMarker = "Not signed in";

    /// <summary>Runs a tool body, mapping sign-in and network failures to <see cref="McpException"/>.</summary>
    public static async Task<T> RunAsync<T>(Func<Task<T>> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            return await body();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(NotSignedInMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new McpException(SignInMessage, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new McpException($"Could not reach Yoto: {ex.Message}. Try again in a moment.", ex);
        }
    }
}
