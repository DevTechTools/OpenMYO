---
title: Edit a playlist
description: Rename tracks, reorder them, set the cover, tidy titles and rename every title with AI in the OpenMyo playlist editor.
ms.topic: how-to
ms.date: 11/16/2026
---

# Edit a playlist

The playlist editor lets you rename tracks, drag to reorder, set the cover and pick icons. Hints point out what needs attention, and each is one click.

## Prerequisites

- You're signed in to Yoto and have at least one playlist.

## Open a playlist and edit it

1. In the library, open a playlist.
2. Rename tracks, drag them to reorder, and set the cover.
3. Select an icon to pick one. See [Match icons to tracks](match-icons.md).
4. Save your changes.

## Use hints

Hints such as "31 missing icons" or "Tidy 8 titles" only appear when they apply. Select a hint to act on it.

## Edit several tracks at once

Select several tracks to give them the same icon, number them or remove them.

## Tidy titles

1. Choose **Tidy Titles…**.
2. Switch each cleanup rule on or off, or add your own find and replace.
3. Check the preview, which shows the result before anything changes.
4. Apply the changes.

Titles that follow a pattern on purpose, like "[051] Episode", are left alone.

## Rename every title with AI

> [!NOTE]
> This is optional and needs an Anthropic API key. See [Match icons to tracks](match-icons.md#set-up-claude-optional).

1. In the editor's **⋯** menu, choose **Rename Every Title with AI**. Claude suggests a clean title for every track, with no track numbers, file-name leftovers or repeated album names.
2. Check the new titles. They show as unsaved edits.
3. Choose **Save**, or **Revert** to undo.

Without an API key, a connected Claude Desktop gets the request instead and saves straight to Yoto. See [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md).

## Related content

- [Match icons to tracks](match-icons.md)
- [Browse and search your library](library-and-search.md)
- [Play audio on your computer or a Yoto player](play-audio.md)
- [Library and editor](../contribute/concepts/library-and-editor.md)
