---
title: Add a Claude Desktop tool
description: Add a new MCP tool that Claude Desktop can call, including its attributes, guard, validation, registration, instructions and tests.
ms.topic: how-to
ms.date: 11/16/2026
---

# Add a Claude Desktop tool

In this article, you add an MCP tool to the connector so Claude Desktop can call it.

## Steps

1. Add a public method with `[McpServerTool(Name = "verb_noun", …)]` and a `[Description]` on the method
   and each parameter, in a `[McpServerToolType]` class in the matching feature folder of [`OpenMyo.Mcp`](../../../src/OpenMyo.Mcp). Set `ReadOnly`, `Destructive` and `Idempotent` to match
   what it does.

1. Wrap the body in [`ToolGuard.RunAsync`](../../../src/OpenMyo.Mcp/Support/ToolGuard.cs). Throw `McpException` with a plain sentence for bad input.

1. Load playlists through the lookup helpers in `Support/`, validate all
   input before saving anything, save once, and return the previous values so Claude can undo.

1. Register a new tool class in [`AddMcpTools`](../../../src/OpenMyo.Mcp/ServiceCollectionExtensions.cs); its methods are found by attribute.

1. Describe when to use it in the `Instructions` text in [`McpEntry`](../../../src/OpenMyo.Mcp/McpEntry.cs).

1. Test it with [`McpWorld`](../../../tests/OpenMyo.Mcp.Tests/McpWorld.cs) in
   [`tests/OpenMyo.Mcp.Tests`](../../../tests/OpenMyo.Mcp.Tests).

## Related content

- [Claude Desktop connector](../concepts/claude-desktop-connector.md)
- [Architecture overview](../architecture/overview.md#projects)
