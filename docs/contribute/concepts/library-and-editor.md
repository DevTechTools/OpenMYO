---
title: Library and editor
description: Understand how the library loads and filters playlists, how the editor saves changes, and how images are cached and drawn.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Library and editor

## Loading the library

[`LibraryViewModel.RefreshAsync`](../../../src/OpenMyo.App/Screens/Library/LibraryViewModel.cs) shows rows as
soon as the listing returns, then loads each card's details in parallel and reports every result back to the
UI thread through `Progress<T>`. Details come from the `updatedAt` card cache when the card is unchanged (see
[Read and save cards](read-and-save-cards.md)).

Each row gets its issue flags from `PlaylistHealth` in Core: no tracks, any chapter without an icon, no
cover. The filter chips are just these flags: "All" clears the filter, problem chips toggle and combine, and
a row shows when it has every chosen issue. When every card is loaded, `ScreenLinks` feeds the icons already
in use into the icon index (see [Where the indexed icons come from](icon-matching.md#where-the-indexed-icons-come-from)).

Selecting several rows enables the bulk actions (match missing icons, delete with a two-click confirm). The
library hands it `SelectedRows` and listens for its events; the bulk-actions view model never holds the
library. Library search also matches track titles. See [Track search](track-search.md).

## Editing a playlist

Selecting one row opens [`PlaylistEditorViewModel`](../../../src/OpenMyo.App/Screens/Editor/PlaylistEditorViewModel.cs).
It keeps the loaded playlist as `_original` and shows its chapters as editable rows. Any change (rename,
drag-reorder, icon, delete) sets `IsDirty`.

- **Save** rebuilds a `Playlist` from `_original` plus the rows (each row keeps its original chapter key) and
  calls `SaveAsync`. The Yoto client turns that into a patch of the raw card (see
  [Read and save cards](read-and-save-cards.md)).
- **Revert** shows `_original` again.
- **Add tracks** and **Change cover** upload first and then save immediately.

The editor's command groups live beside it in `Screens/Editor/`. They reach the editor only through
`IChapterEditor` (run work, read the current playlist, apply changed chapters), which keeps the editor file
small.

The icon picker is hidden until the user clicks a track's icon (or while it asks for an API key an AI action
needs). The chapter selection knows nothing about the picker: while it is open, the editor points the
selection at the picker's target and, after each pick, advances to the next chapter without an icon (shared
with Import).

## Images

`RemoteImageCache` downloads each icon and cover URL once, keeping bytes on disk and decoded bitmaps in
memory. Concurrent requests for one URL share a download. `PixelImage` draws 16×16 icons scaled up without
blurring.

## Related content

- [Read and save cards](read-and-save-cards.md)
- [Navigation and commands](navigation-and-commands.md)
- [Track search](track-search.md)
- [Edit playlists](../../user/edit-playlists.md)
