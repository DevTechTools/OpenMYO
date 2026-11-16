---
title: Podcast feed import
description: Understand how the From Feed dialog previews a podcast, filters episodes, downloads them in parallel and builds a draft.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Podcast feed import

The **From Feed…** dialog previews a podcast before anything downloads, then turns the chosen episodes into
a draft for the [import pipeline](import-pipeline.md).

## Flow

1. [`FeedUrl`](../../../src/OpenMyo.Core/Feeds/FeedUrl.cs) accepts http(s) links, `feed://`/`itpc://`
   subscribe links and bare addresses. A link pasted anywhere on the import screen opens the dialog with it.
2. `FeedParser` reads RSS 2.0 with the iTunes extensions. Only items with an audio enclosure count. Episodes
   are put oldest first, by `pubDate` when every item has one, otherwise in reverse feed order. DTDs and
   external entities are never resolved.
3. The user ticks episodes (a cap applies to how many can be downloaded at once) and can narrow the list
   with length limits (episodes with no length in the feed pass) and words to include or exclude.
   Filtered-out episodes are hidden and unticked. One order toggle sets both the list order and the track
   order on the card.
4. `FeedImporter` downloads them in parallel (a setting; each row shows its own progress) into the cache
   folder, with the show artwork as the cover (type sniffed from the bytes; a broken image link only loses
   the cover). It builds the draft directly, not through `DraftBuilder`: the feed's order and titles are more
   reliable than podcast tags. Titles still go through `TitleCleaner`, and the show title becomes the
   playlist title.
5. The draft joins the import screen like any other. Its download folder is deleted when the draft is
   published, discarded or cleared; anything left behind (a crash, the app closed) is swept on a later feed
   download.

## Related content

- [Import pipeline](import-pipeline.md)
- [Cross-cutting concerns](../architecture/cross-cutting-concerns.md#caching)
- [Import a podcast feed](../../user/import-podcast-feed.md)
