---
title: Add domain logic
description: Add a new title clean-up rule, library filter, matcher or similar pure logic to OpenMyo.Core, register it, test it and call it from the UI or a tool.
ms.topic: how-to
ms.date: 11/16/2026
---

# Add domain logic

In this article, you add pure logic to `OpenMyo.Core`, such as a new title clean-up rule, library filter or matcher, and wire it up so the app or a Claude Desktop tool can call it.

## Steps

1. Put the logic in the feature folder under [`OpenMyo.Core`](../../../src/OpenMyo.Core) as a pure class or
   static method. Take dependencies through Core interfaces only.

1. Register it in [`AddCore`](../../../src/OpenMyo.Core/ServiceCollectionExtensions.cs) if it has state or
   dependencies.

1. Test it in the matching folder of [`tests/OpenMyo.Core.Tests`](../../../tests/OpenMyo.Core.Tests), using the
   fakes in [`Fakes`](../../../tests/OpenMyo.Core.Tests/Fakes).

1. Call it from a view model ([Add a page to the navigation rail](add-navigation-page.md), [Add a panel to an existing screen](add-panel.md)) or an MCP tool
   ([Add a Claude Desktop tool](add-claude-desktop-tool.md)).

## Examples

- **Title clean-up rule:** a property on [`TitleCleanupOptions`](../../../src/OpenMyo.Core/Text/TitleCleanupOptions.cs) and a step in [`TitleCleaner.Clean`](../../../src/OpenMyo.Core/Text/TitleCleaner.cs).
- **Library filter:** a flag in [`PlaylistIssues`](../../../src/OpenMyo.Core/Library/PlaylistIssues.cs), a check in [`PlaylistHealth`](../../../src/OpenMyo.Core/Library/PlaylistHealth.cs), and a chip in
  `LibraryViewModel.Chips` ([`LibraryViewModel`](../../../src/OpenMyo.App/Screens/Library/LibraryViewModel.cs)); matching and counts come from the flag.

## Related content

- [Architecture overview](../architecture/overview.md#projects)
- [Invariants](../architecture/invariants.md)
- [Improve icon matches](improve-icon-matches.md)
