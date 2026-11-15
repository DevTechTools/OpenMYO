using Microsoft.Extensions.DependencyInjection;
using OpenMyo.Core.Abstractions;
using OpenMyo.Mcp;

namespace OpenMyo.App.Composition;

/// <summary>
/// Runs OpenMyo without a window as Claude Desktop's connector: the same services as the app, served over
/// stdio. Nothing but MCP messages may reach stdout, so this mode never starts Avalonia or writes output.
/// </summary>
internal static class McpMode
{
    public static async Task<int> RunAsync()
    {
        await using var services = ServiceRegistration.Build();

        // Tools report "sign in to Yoto first" themselves when this fails, so a signed-out user gets a clear answer.
        await services.GetRequiredService<IYotoSession>().TryRestoreAsync(CancellationToken.None);
        await McpEntry.RunAsync(services, CancellationToken.None);
        return 0;
    }
}
