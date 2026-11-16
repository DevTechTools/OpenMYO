---
title: Render a screen without signing in
description: Render OpenMyo screens headlessly to PNG with sample data so you can check UI changes without a Yoto account.
ms.topic: how-to
ms.date: 11/16/2026
---

# Render a screen without signing in

In this article, you render a screen to a PNG with sample data, so you can check a UI change without a Yoto account.

## Steps

1. Add a block in [`tools/OpenMyo.Snapshots`](../../../tools/OpenMyo.Snapshots) that opens your screen's view model.

1. Run the tool:

   ```bash
   dotnet run --project tools/OpenMyo.Snapshots -- <output folder> [card json]
   ```

## Related content

- [Architecture overview](../architecture/overview.md#projects)
- [Add a panel to an existing screen](add-panel.md)
- [Add a page to the navigation rail](add-navigation-page.md)
