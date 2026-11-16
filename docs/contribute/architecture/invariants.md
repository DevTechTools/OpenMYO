---
title: Architecture invariants
description: The rules about dependencies, card saving, tokens, Claude and error handling that every change to OpenMyo must keep true.
ms.topic: reference
ms.date: 11/16/2026
---

# Architecture invariants

Check your change against these rules before you open a pull request.

- **Core references no other OpenMyo project** and no UI, HTTP or platform package.
- **Only `OpenMyo.App` knows concrete types.** Libraries register their services through one
  `AddXxx()` method each.
- **Domain records are never mutated.** Every edit creates a new record with `with { … }`.
- **A save never rebuilds a card.** It patches the last card fetched from Yoto, so fields OpenMyo does not
  model survive.
- **The refresh token is only read or used inside both the in-process gate and [`RefreshLock`](../../../src/OpenMyo.Yoto/Auth/RefreshLock.cs).** Yoto
  refresh tokens are single-use; two concurrent refreshes sign the user out.
- **The bearer token is only sent by the `yoto-api` client.** Upload storage, yotoicons.com and Anthropic
  never see it.
- **Claude never chooses an icon.** It returns keywords that are matched locally, and icon matching works
  without an Anthropic key.
- **In connector mode nothing but MCP messages is written to stdout**, so Avalonia never starts.
- **Services throw; only the UI boundary catches everything.** The catch-alls are
  [`OperationViewModel.RunAsync`](../../../src/OpenMyo.App/Screens/OperationViewModel.cs) (user actions),
  [`BackgroundWork.Forget`](../../../src/OpenMyo.App/Services/BackgroundWork.cs) (work nobody awaits) and
  [`IconBitmapLoader`](../../../src/OpenMyo.App/Services/IconBitmapLoader.cs) (pictures). Each one logs the failure.
- **View models never reference each other sideways.** A parent owns its children; children raise events;
  links between pages live in [`ScreenLinks`](../../../src/OpenMyo.App/Composition/ScreenLinks.cs).
- **Secrets never appear in settings, logs or URLs.**

## Related content

- [Architecture overview](overview.md)
- [Cross-cutting concerns](cross-cutting-concerns.md)
- [Design decisions](../reference/design-decisions.md)
- [Read and save cards](../concepts/read-and-save-cards.md)
- [Sign-in and tokens](../concepts/sign-in-and-tokens.md)
