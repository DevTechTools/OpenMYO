---
title: Match icons to tracks
description: Match track titles to Yoto icons for free, search community icons, and optionally use Claude to find better matches.
ms.topic: how-to
ms.date: 11/16/2026
---

# Match icons to tracks

OpenMyo matches track titles to icons so each track on your player shows a picture. Matching is free and doesn't use AI. You can add community icons, and optionally ask Claude for more ideas.

## Prerequisites

- You're signed in to Yoto.
- For **Search with AI** only: an Anthropic API key.

## Match icons for free

OpenMyo compares each track title with the names and tags of Yoto's public icons, the icons already in your playlists and Yoto's number icons. If Yoto has nothing close, a strong match from [yotoicons.com](https://yotoicons.com) is added to your icons.

1. Select one or more playlists in the library, or open a playlist in the editor.
2. Choose the icon matching action, or select a hint such as "12 missing icons".
3. Check the result. Select a track's icon to change it in the icon picker.

## Search community icons

The icon picker also searches yotoicons.com, credits the artist and adds the icon you choose to your Yoto account.

The search runs against a copy of the catalog kept on your computer and updated automatically, so results appear as you type. Images are only downloaded when shown.

## Set up Claude (optional)

Icon matching works without Claude. To add **Search with AI** to the icon picker:

1. Open **Settings**.
2. Turn on **AI Icon Search (optional)**.
3. Paste an API key from the Anthropic console into **Anthropic API key**. OpenMyo stores it in the Keychain or DPAPI, and the search runs as soon as the key is saved.
4. Optional: choose a different Claude model.

> [!IMPORTANT]
> Claude never sees your audio, only track titles.

## Search with AI

1. In any icon picker, select **Search with AI**. Claude only runs when you click it.
2. Claude suggests a few words for that one title. OpenMyo matches them against the local icon index.
3. Pick an icon from the results.

Answers are cached per title. In the editor's **⋯** menu, **Re-pick Every Icon with AI** asks Claude about titles the free matcher is unsure of.

## Related content

- [Edit a playlist](edit-playlists.md)
- [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md)
- [Icon matching](../contribute/concepts/icon-matching.md)
