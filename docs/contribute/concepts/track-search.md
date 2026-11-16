---
title: Track search
description: Understand how the library search matches playlist and track titles together without extra network requests.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Track search

The library already loads every playlist's chapters in the background (from the `updatedAt` card cache), so
search makes no extra requests. See [Library and editor](library-and-editor.md#loading-the-library).

- [`TrackSearch.Match`](../../../src/OpenMyo.Core/Library/TrackSearch.cs) checks the playlist title and each
  chapter title. A title matches when it contains every word, ignoring case and accents, and the matched
  ranges are returned for highlighting. A chapter is a track to the user, so its 1-based position is the
  track number shown.
- Playlists whose title matches come first, then those that match only on tracks. Clicking a matching track
  opens the playlist with that track selected and scrolled into view.
- Searching while details are still loading inserts newly matching rows in place, rather than rebuilding the
  list, so the playlist open in the editor keeps its unsaved edits. The summary line says how many playlists
  are still being checked, so an incomplete result isn't mistaken for "no results".
- The MCP tool `search_tracks` uses the same matching. See [Claude Desktop connector](claude-desktop-connector.md).

## Related content

- [Library and editor](library-and-editor.md)
- [Claude Desktop connector](claude-desktop-connector.md)
- [Search and filter the library](../../user/library-and-search.md)
