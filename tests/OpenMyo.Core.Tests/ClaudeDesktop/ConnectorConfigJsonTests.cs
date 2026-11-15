using System.Text.Json.Nodes;
using OpenMyo.Core.ClaudeDesktop;

namespace OpenMyo.Core.Tests.ClaudeDesktop;

public sealed class ConnectorConfigJsonTests
{
    private const string Existing = """{ "theme": "dark", "mcpServers": { "files": { "command": "npx" } } }""";

    [Fact]
    public void WithServer_ExistingConfig_KeepsOtherSettingsAndServers()
    {
        var json = JsonNode.Parse(ConnectorConfigJson.WithServer(Existing, "openmyo", "/Apps/OpenMyo", ["--mcp"]))!;

        Assert.Equal("dark", json["theme"]!.GetValue<string>());
        Assert.Equal("npx", json["mcpServers"]!["files"]!["command"]!.GetValue<string>());
        Assert.Equal("--mcp", json["mcpServers"]!["openmyo"]!["args"]![0]!.GetValue<string>());
    }

    [Fact]
    public void WithServer_NoFile_CreatesServersSection() =>
        Assert.True(ConnectorConfigJson.HasServer(ConnectorConfigJson.WithServer(null, "openmyo", "/Apps/OpenMyo", []), "openmyo"));

    [Fact]
    public void WithoutServer_RemovesOnlyThatServer()
    {
        var json = ConnectorConfigJson.WithoutServer(ConnectorConfigJson.WithServer(Existing, "openmyo", "x", []), "openmyo");

        Assert.False(ConnectorConfigJson.HasServer(json, "openmyo"));
        Assert.True(ConnectorConfigJson.HasServer(json, "files"));
    }

    [Fact]
    public void WithServer_NotAnObject_Throws() =>
        Assert.Throws<InvalidDataException>(() => ConnectorConfigJson.WithServer("[]", "openmyo", "x", []));
}
