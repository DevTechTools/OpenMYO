---
title: Build the app
description: Publish OpenMyo as a single file that runs without .NET installed, for Windows or macOS on Apple silicon or Intel.
ms.topic: how-to
ms.date: 11/16/2026
---

# Build the app

There are no prebuilt downloads yet, so you build the app yourself. On macOS you can build a normal `.app` bundle; on either platform you can build a single executable. Both run without .NET installed.

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download).
- A clone of the OpenMyo repository.

## Build a macOS app bundle

Use this on a Mac to get an `OpenMyo.app` you can drag into **Applications**.

1. From the repository root, run:

   ```bash
   bash eng/publish-macos-app.sh
   ```

   The script builds for this Mac's processor. To build for the other one, add `arm64` (Apple silicon) or
   `x64` (Intel).

1. Find the results in `publish/`: `OpenMyo.app`, and a zip of it to copy to another Mac.

1. Drag `OpenMyo.app` into **Applications** and open it.

> [!NOTE]
> The bundle has an ad-hoc signature, not a Developer ID one. It opens normally on the Mac that built it.
> On another Mac, macOS blocks the first launch: open it, then go to **System Settings** >
> **Privacy & Security** and choose **Open Anyway**.

## Build a single executable

1. From the repository root, run the command for your platform. The output goes to `publish/`.

   | Platform | Command | Output |
   |---|---|---|
   | Windows | `dotnet publish src/OpenMyo.App -c Release -r win-x64 -o publish` | `OpenMyo.exe` |
   | macOS, Apple silicon | `dotnet publish src/OpenMyo.App -c Release -r osx-arm64 -o publish` | `OpenMyo` |
   | macOS, Intel | `dotnet publish src/OpenMyo.App -c Release -r osx-x64 -o publish` | `OpenMyo` |

   You can build for either platform from either one.

2. Open the result. On macOS, run it from Terminal (`./publish/OpenMyo`) or double-click it in Finder. It opens a Terminal window too; use the app bundle to avoid that.

## Things to know

- The builds aren't signed with a Developer ID or Windows certificate. Windows SmartScreen and macOS Gatekeeper warn the first time you open a copy downloaded from elsewhere.
- The first launch takes a few seconds longer while the app unpacks itself.

## Use the built app with Claude Desktop

Open the built app and choose **Settings** > **Connect to Claude Desktop**. Claude Desktop then launches that same app, so keep it where it is, or connect again after you move it. If you switch from the single executable to the app bundle, connect again from the bundle. For details, see [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md).

## Related content

- [Quickstart: Sign in and run OpenMyo](quickstart.md)
- [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md)
- [Data and cache locations](reference/data-locations.md)
