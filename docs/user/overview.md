---
title: OpenMyo overview
description: OpenMyo is an unofficial desktop app for macOS and Windows that manages Yoto Make Your Own playlists, titles and icons.
ms.topic: overview
ms.date: 11/16/2026
---

# OpenMyo overview

OpenMyo is an unofficial, open-source desktop app for managing Yoto "Make Your Own" (MYO) playlists on macOS and Windows. It imports folders of audio as playlists, cleans up track titles and matches icons. It searches every playlist for a track by title and can play any track on one of your Yoto players. Claude Desktop can also use it to rename tracks, pick icons, find tracks and play them on a player.

> [!NOTE]
> OpenMyo isn't affiliated with or endorsed by Yoto. Each user signs in with their own Yoto account and their own registered Yoto app.

## What you can do

- **Import in bulk.** Choose or drag in folders or audio files, and each folder becomes a playlist. Titles are cleaned, tracks are ordered and artwork is found. See [Import folders of audio as playlists](import-folders.md).
- **Import a podcast feed.** Pick episodes from a feed and turn them into a playlist. See [Import a podcast feed as a playlist](import-podcast-feed.md).
- **Manage your library.** Browse playlists as a list or a grid of covers, filter by what's missing, search playlists and tracks, and act on several at once. See [Browse and search your library](library-and-search.md).
- **Edit playlists.** Rename tracks, drag to reorder, set the cover, tidy titles and pick icons. See [Edit a playlist](edit-playlists.md).
- **Match icons.** Match track titles to Yoto's public icons for free, or add community icons and optional AI search. See [Match icons to tracks](match-icons.md).
- **Play audio.** Listen to tracks on your computer or on a Yoto player. See [Play audio on your computer or a Yoto player](play-audio.md).
- **Work with Claude Desktop.** Let Claude rename playlists and tracks, pick icons, find tracks and play them. See [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md).

To install it, start with [Quickstart: Sign in and run OpenMyo](quickstart.md).

## Privacy

OpenMyo keeps your Yoto sign-in and API key in the macOS Keychain or encrypted with Windows DPAPI, never in settings files. Claude is optional, and it never sees your audio, only track titles. For where everything else is kept, see [Data and cache locations](reference/data-locations.md).

## Related content

- [Quickstart: Sign in and run OpenMyo](quickstart.md)
- [Build the app](build-the-app.md)
- [Architecture overview](../contribute/architecture/overview.md)
