using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace OpenMyo.Mcp.Support;

/// <summary>Finds the <c>[McpServerTool]</c> methods on this assembly's tool classes and binds them to DI instances.</summary>
public static class ToolRegistry
{
    /// <summary>Builds the tool collection; each call resolves its tool class from <paramref name="services"/>.</summary>
    public static McpServerPrimitiveCollection<McpServerTool> Build(IServiceProvider services)
    {
        var tools = new McpServerPrimitiveCollection<McpServerTool>();
        var types = typeof(ToolRegistry).Assembly.GetTypes().Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);
            foreach (var method in methods)
            {
                tools.Add(McpServerTool.Create(method, _ => services.GetRequiredService(type), new McpServerToolCreateOptions { Services = services }));
            }
        }

        return tools;
    }
}
