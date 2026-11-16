---
title: App start-up and composition
description: Understand how OpenMyo starts in window or connector mode, builds its services, restores the Yoto session and reports UI errors.
ms.topic: concept-article
ms.date: 11/16/2026
---

# App start-up and composition

`OpenMyo.App` hosts both the Avalonia window and the windowless MCP connector that Claude Desktop starts.
Both build the same services, so a feature in Core is available to the screens and the tools alike.

## Launch modes

[`Program.Main`](../../../src/OpenMyo.App/Program.cs) picks one of two modes:

- **Window mode** (normal): starts Avalonia and shows the main window.
- **Connector mode** (`--mcp`): builds the *same* services, silently restores the Yoto session, and serves
  MCP over stdio. Avalonia never starts, and nothing else may write to stdout. See
  [Claude Desktop connector](claude-desktop-connector.md).

## Composition root

[`ServiceRegistration.Build`](../../../src/OpenMyo.App/Composition/ServiceRegistration.cs) is the composition
root. It calls each project's `AddXxx()` and registers the view models. Two details:

- Almost everything is a **singleton**. The caches and shared in-flight loads only work
  because there is one instance per process.
- Every `HttpClient` gets a `User-Agent`, because Yoto's media CDN returns 403 without one.

## What happens at launch

`MainViewModel.InitializeAsync` loads settings, then tries to restore the Yoto session. If a stored refresh
token still works, the shell opens the library. If not, the app opens on Settings so the user can enter a
client id and sign in. See [Sign-in and tokens](sign-in-and-tokens.md).

## How the UI handles work and errors

Every screen view model derives from
[`OperationViewModel`](../../../src/OpenMyo.App/Screens/OperationViewModel.cs). Its `RunAsync(status, work)`
sets `IsBusy`, gives the work a `CancellationToken` wired to the Cancel button, and catches *all*
exceptions, showing the message in `Error`. This is the one deliberate catch-all in the codebase:
services below it throw, and the UI boundary reports.

## Related content

- [Navigation and commands](navigation-and-commands.md)
- [Claude Desktop connector](claude-desktop-connector.md)
- [Cross-cutting concerns](../architecture/cross-cutting-concerns.md)
