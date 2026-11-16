---
description: Pointer to the OpenMyo.App documentation; read when adding or changing a screen, panel, view model or style.
---

# OpenMyo.App

The Avalonia desktop UI and the composition root. Screens follow MVVM: a view model holds state and
commands, a view (`.axaml`) binds to it with compiled bindings, and nothing in a view calls services.
The same executable also runs the Claude Desktop connector when started with `--mcp`.

The full guide to this project is in the docs:

- [Start-up and composition](../../docs/contribute/concepts/app-startup.md),
  [navigation and commands](../../docs/contribute/concepts/navigation-and-commands.md) and
  [library and editor](../../docs/contribute/concepts/library-and-editor.md).
- How-to guides: [add a page to the navigation rail](../../docs/contribute/how-to/add-navigation-page.md),
  [add a panel to a screen](../../docs/contribute/how-to/add-panel.md),
  [add a setting or a secret](../../docs/contribute/how-to/add-setting-or-secret.md).

The UI rules every screen is reviewed against are in [CODESTANDARDS.md §8](../../CODESTANDARDS.md).
