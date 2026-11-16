---
title: "Quickstart: Sign in and run OpenMyo"
description: Register a Yoto app, start OpenMyo with dotnet run and sign in with your Yoto account to see your playlists.
ms.topic: quickstart
ms.date: 11/16/2026
---

# Quickstart: Sign in and run OpenMyo

In this quickstart, you register your own Yoto app, start OpenMyo from source and sign in so your playlists appear.

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download). The exact version is pinned in `global.json`.
- macOS or Windows.
- A Yoto account.

## Register an app at yoto.dev

1. Go to <https://yoto.dev> and register an app as a **public client**.
2. Set the redirect URI to `http://127.0.0.1:8787/callback`.
3. Copy the app's client id.

## Start the app

From the repository root, run:

```bash
dotnet run --project src/OpenMyo.App
```

## Sign in

1. Open **Settings**.
2. Paste the client id into **Yoto client ID**.
3. Select **Sign In…**. OpenMyo opens your browser so you can sign in with Yoto and allow it to manage your playlists and play them on your Yoto players. Your password never passes through the app.

When you're signed in, **Settings** shows **Signed in**.

## Next steps

- [Import folders of audio as playlists](import-folders.md)
- [Browse and search your library](library-and-search.md)
- [Build a standalone copy of the app](build-the-app.md)
