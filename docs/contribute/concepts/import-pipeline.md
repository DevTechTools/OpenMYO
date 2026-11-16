---
title: Import pipeline
description: Understand how OpenMyo turns dropped folders and files into playlists, from grouping and track order to title cleaning, artwork and publishing.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Import pipeline

The user drops folders or files on the import screen.
[`DraftBuilder`](../../../src/OpenMyo.Core/Import/DraftBuilder.cs) turns them into playlist drafts with no
network access. Then the screen asks for icon suggestions, the user reviews, and
[`PlaylistPublisher`](../../../src/OpenMyo.Core/Publishing/PlaylistPublisher.cs) uploads and creates each
playlist. Podcast feeds skip `DraftBuilder` but join the same screen and publishing path. See
[Podcast feed import](podcast-feed-import.md).

## Which files become which playlist

- Dropped folders are expanded recursively to audio files; hidden files are skipped.
- **Every folder that directly holds audio becomes one playlist.**
- **Disc folders merge into their parent.** A folder named like `CD1`, `Disc 2` or `disk_03` belongs to the
  folder above it, so a two-CD audiobook becomes one playlist.

## Track order

Tracks sort by disc first (disc tag, else the `CD 2` folder number, else 1). Within a disc the importer uses
the most trustworthy signal that **every** file has:

1. Track-number tags, but only if all files have one and no two clash on the same disc.
2. Otherwise, a leading number in every file name (`01 …`, `Track 3`, `2-05 …`).
3. Otherwise, natural file-name order (so `2` sorts before `10`).

"All or nothing" matters: mixing tagged and untagged files would interleave them.

## Titles

The raw title for each track is the tag title, unless it is empty or a placeholder like `Track 01`, in which
case the file name. The playlist title is the album tag if every track agrees on it, otherwise the folder
name.

[`TitleCleaner.CleanAll`](../../../src/OpenMyo.Core/Text/TitleCleaner.cs) then cleans the whole list together.
Each rule can be switched off in `TitleCleanupOptions` (the editor's clean-up panel exposes them). The rules
cover:

- separators (`_`, `%20`, dots when there are no spaces) and web addresses;
- noise in brackets (`(Official Audio)`, `[320 kbps]`) and anything in `[square brackets]`;
- numbering (`CD1 `, `1-03 `, `03 - `, `Track 3.`);
- a bare leading number: removed if zero-padded (`01 Song`), otherwise only when **every** title starts with
  a digit, so `99 Red Balloons` survives in a normal list;
- ALL CAPS or all lower case → Title Case;
- user find/replace, and a shared `Artist - ` prefix on every title.

If cleaning leaves nothing, the raw title is kept, and results are cut to Yoto's 140-character limit. The
bracket rule is turned off when most titles start with `[`, because then `[051] Connie` is the user's own
numbering, not junk.

## Artwork

Artwork sources are a file, or "the picture embedded in this audio file". Nothing is read until publishing.

- **Cover**: an image named like `cover`, `folder` or `front`; else the only image in the folder that is not
  a track sidecar; else the first embedded picture.
- **Per-track icon art**: an image with the same base name as the audio file (`03 Owl.mp3` + `03 Owl.png`).
  Embedded pictures count only when tracks carry *different* pictures; identical embedded art is the album
  cover, not an icon.

## Publishing

- Tracks upload in parallel (a setting). Results go into a fixed array by index, so parallelism never
  changes the order.
- For each track, the icon is the chosen one. If none was chosen, the per-track icon art is uploaded as a
  custom icon; if there is none, the track has no icon.
- Each track becomes one chapter with one track, keys `01`, `02`, … The cover is uploaded, then one
  `SaveAsync` creates the card.
- "Add tracks" in the editor does the same via `PlaylistMediaService`, numbering after the existing chapters,
  and runs added files through `DraftBuilder` so they are cleaned and ordered like an import.

## Related content

- [Podcast feed import](podcast-feed-import.md)
- [Icon matching](icon-matching.md)
- [Upload media](media-uploads.md)
- [Import folders of audio as playlists](../../user/import-folders.md)
