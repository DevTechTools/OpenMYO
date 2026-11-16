---
title: Media uploads
description: Understand how OpenMyo uploads audio, cover images and icons to Yoto, including hashing, transcoding polls and progress.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Media uploads

[`YotoMediaUploader`](../../../src/OpenMyo.Yoto/Media/YotoMediaUploader.cs) sends audio, cover images and icons
to Yoto.

## Audio

`UploadAudioAsync` works in five steps:

1. Hash the file with SHA-256.
2. `GET media/transcode/audio/uploadUrl?sha256=…`. Yoto returns an upload id and, *only if it has never
   seen these bytes*, a presigned upload URL. Re-importing the same file skips the upload entirely.
3. `PUT` the file to the presigned URL on the `yoto-upload` client, reporting bytes sent. Upload counts for
   most of the progress bar; transcoding is the rest.
4. Poll `media/upload/{id}/transcoded` (404 means "not yet") with backoff until a timeout.
5. Return `yoto:#<transcodedSha256>` with the format, duration, size and channels Yoto reports.

## Cover

`POST media/coverImage/user/me/upload` returns a public URL that goes in `metadata.cover.imageL`.

## Icon

`POST media/displayIcons/user/me/upload?autoConvert=true` (Yoto scales it to 16×16) returns a media id. The
uploader then tells the icon catalog to forget the user's icon list so the new icon shows up in searches.

## Related content

- [Yoto HTTP pipeline](yoto-http-pipeline.md)
- [Import pipeline](import-pipeline.md)
- [Card model](card-model.md)
