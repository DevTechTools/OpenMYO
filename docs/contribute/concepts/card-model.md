---
title: Card model
description: Understand how a Yoto Make Your Own card is structured and how OpenMyo maps it to immutable domain records.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Card model

Everything OpenMyo does ends up as a change to a **card** on Yoto's `/content` API.

## Structure of a card

A Make Your Own card is one JSON document:

```text
card
├─ cardId, title, updatedAt, …            (plus fields OpenMyo does not model)
├─ metadata.cover.imageL                   cover image URL
└─ content.chapters[]                      what the player steps through
   ├─ key "01", title, overlayLabel "1"
   ├─ display.icon16x16 "yoto:#<mediaId>"  the 16×16 pixel icon the player shows
   └─ tracks[]
      ├─ key "01", title
      ├─ trackUrl "yoto:#<sha256>"         transcoded audio held by Yoto
      └─ format, duration, fileSize, channels, display.icon16x16
```

## Properties that shape the code

- **A chapter is what the user sees as "a track".** MYO cards normally have one track per chapter.
  OpenMyo creates exactly that shape, renaming a single-track chapter renames its track too, and setting an
  icon sets it on the chapter *and* each of its tracks.
- **Icons are referenced by media id.** Yoto's public icons and the user's own uploads live in the same id
  space, so assigning an icon is just writing `yoto:#<id>`.
- **Audio is referenced by content hash.** Uploading a file gives back `yoto:#<sha256 of the transcode>`.
- **Saving is whole-document.** `POST /content` with a `cardId` replaces the card. There is no
  per-field PATCH, so OpenMyo saves by patching the last-fetched document (see [Read and save cards](read-and-save-cards.md)).
- **Titles are capped at 140 characters.** Yoto rejects the whole save for one over-long chapter title
  without saying which.

## Domain records

The domain mirrors this as immutable records in `src/OpenMyo.Core/Playlists/`: `Playlist` → `Chapter` →
`Track`, plus `PlaylistSummary` for the cheap library listing. Every edit produces a new record
(`with { … }`); nothing mutates a playlist in place.

## Related content

- [Read and save cards](read-and-save-cards.md)
- [Yoto HTTP pipeline](yoto-http-pipeline.md)
- [Upload media](media-uploads.md)
- [Architecture overview](../architecture/overview.md)
