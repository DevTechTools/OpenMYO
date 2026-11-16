---
title: Add a setting or a secret
description: Add a new non-secret setting to AppSettings and the Settings screen, or a new secret to the OS secret store, without leaking it into settings or logs.
ms.topic: how-to
ms.date: 11/16/2026
---

# Add a setting or a secret

In this article, you add either a setting (anything that isn't secret) or a secret (tokens and keys).

> [!IMPORTANT]
> Never put a secret in `AppSettings`, a log or a URL.

## Add a setting

1. Add an `init` property with a default to [`AppSettings`](../../../src/OpenMyo.Core/Abstractions/ISettingsStore.cs).
   Existing `settings.json` files get the default.

1. Read it with [`ISettingsStore.LoadAsync`](../../../src/OpenMyo.Core/Abstractions/ISettingsStore.cs); save with `SaveAsync(current with { … })`.

1. Show it in [`SettingsViewModel`](../../../src/OpenMyo.App/Screens/Settings/SettingsViewModel.cs) and
   [`SettingsView`](../../../src/OpenMyo.App/Screens/Settings/SettingsView.axaml).

## Add a secret

1. Add a name to [`SecretNames`](../../../src/OpenMyo.Core/Abstractions/SecretNames.cs).

1. Read and write it through [`ISecretStore`](../../../src/OpenMyo.Core/Abstractions/ISecretStore.cs).

## Related content

- [Invariants](../architecture/invariants.md)
- [Data locations (user reference)](../../user/reference/data-locations.md)
- [Navigation and commands](../concepts/navigation-and-commands.md)
