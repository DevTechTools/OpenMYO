using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenMyo.Core.ClaudeDesktop;

/// <summary>
/// Edits the "mcpServers" section of Claude Desktop's claude_desktop_config.json without touching anything
/// else in it: the user's other connectors and preferences belong to them.
/// </summary>
public static class ConnectorConfigJson
{
    /// <summary>OpenMyo's own entry in the "mcpServers" section.</summary>
    public const string OpenMyoServer = "openmyo";

    private const string ServersKey = "mcpServers";
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static bool HasServer(string? json, string name) =>
        Parse(json)[ServersKey]?.AsObject().ContainsKey(name) == true;

    /// <summary>Returns the config with <paramref name="name"/> set to run <paramref name="command"/>.</summary>
    public static string WithServer(string? json, string name, string command, IReadOnlyList<string> args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(args);

        var root = Parse(json);
        if (root[ServersKey] is not JsonObject servers)
        {
            servers = [];
            root[ServersKey] = servers;
        }

        servers[name] = new JsonObject
        {
            ["command"] = command,
            ["args"] = new JsonArray(args.Select(arg => (JsonNode?)JsonValue.Create(arg)).ToArray()),
        };
        return root.ToJsonString(Indented);
    }

    public static string WithoutServer(string? json, string name)
    {
        var root = Parse(json);
        (root[ServersKey] as JsonObject)?.Remove(name);
        return root.ToJsonString(Indented);
    }

    private static JsonObject Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidDataException("Claude Desktop's config file is not a JSON object; fix or remove it, then try again.");
    }
}
