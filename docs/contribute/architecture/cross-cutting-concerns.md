---
title: Cross-cutting concerns
description: Understand how OpenMyo handles async work, concurrency, errors, security, the Avalonia UI and testing across every project.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Cross-cutting concerns

These conventions apply in every project. The mandatory rules are in [CODESTANDARDS.md](../../../CODESTANDARDS.md); this page explains the reasons.

## Async and cancellation

All I/O is async and takes a `CancellationToken`. Long operations in the UI
run through [`OperationViewModel.RunAsync`](../../../src/OpenMyo.App/Screens/OperationViewModel.cs), which owns busy state, the Cancel button and error display.
Work that is started but not awaited (a search as the user types, a refresh after an event) goes through
`Forget(logger, description)` so its failure is logged rather than lost.

## Concurrency

Parallel work is always bounded: uploads by the `MaxParallelUploads` setting, and library loads, yotoicons.com
pages and icon imports by fixed small limits. Results are written by index, so
parallelism never changes order. Shared loads (icon catalog, icon index, community catalog) are
singletons that hand every caller the same in-flight `Task`.

## Errors

Yoto responses that fail become [`YotoApiException`](../../../src/OpenMyo.Yoto/Http/YotoApiException.cs). The `yoto-api` client retries 429 and 503
a few times and refreshes the token once on 401. Optional sources (yotoicons.com, Anthropic) fail
soft: matching carries on without them. At the UI boundary every failure is logged through `ILogger` with
its full exception, and the user sees a plain sentence instead: "Couldn't reach the server" for network
errors, a permissions or file hint for disk errors, otherwise the exception's own message.

## Security

OAuth uses Authorization Code with PKCE on a loopback redirect and a checked `state`; each
user registers their own public client, so the binary holds no client secret. Tokens and the API key live
in the OS secret store. Card ids, media ids and title lengths are validated before they reach a URL or a
save. Claude receives track titles and the icon vocabulary only.

## UI (Avalonia)

The screens use Avalonia 11 with MVVM. Views bind to view models with compiled
bindings (`AvaloniaUseCompiledBindingsByDefault` and `x:DataType`), so a wrong binding path fails the build.
Buttons and menu items run `ICommand`s that CommunityToolkit.Mvvm generates from `[RelayCommand]` methods;
`Click` handlers are only for view-only work such as focus and drag-and-drop. Avalonia draws its own
controls, so a screen looks and behaves the same on macOS and Windows. See [Commands](../concepts/navigation-and-commands.md#commands).

## Caching

OpenMyo caches card JSON, Yoto's public icon list, the yotoicons.com catalog and icon and cover images on
disk, and keeps the user's own icons, the icon search index and Claude keywords in memory. Cards are reused
while their `updatedAt` is unchanged (see [Read and save cards](../concepts/read-and-save-cards.md)). The
cache folder is disposable: deleting it only costs a re-download. For where the folders are, see
[Data locations](../../user/reference/data-locations.md).

## Testing

Tests never touch the network or a real keychain. Core logic is tested with fakes of the
Core interfaces; the Yoto client is tested against stubbed HTTP responses.

## Related content

- [Invariants](invariants.md)
- [Data locations (user reference)](../../user/reference/data-locations.md)
- [Yoto HTTP pipeline](../concepts/yoto-http-pipeline.md)
- [Sign-in and tokens](../concepts/sign-in-and-tokens.md)
