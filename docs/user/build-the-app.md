---
title: Build the app
description: Publish OpenMyo as a single file that runs without .NET installed, for Windows or macOS on Apple silicon or Intel.
ms.topic: how-to
ms.date: 11/16/2026
---

# Build the app

There are no prebuilt downloads yet, so you build the app yourself. Each command makes one file that runs without .NET installed.

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download).
- A clone of the OpenMyo repository.

## Publish

1. From the repository root, run the command for your platform. The output goes to `publish/`.

   | Platform | Command | Output |
   |---|---|---|
   | Windows | `dotnet publish src/OpenMyo.App -c Release -r win-x64 -o publish` | `OpenMyo.exe` |
   | macOS, Apple silicon | `dotnet publish src/OpenMyo.App -c Release -r osx-arm64 -o publish` | `OpenMyo` |
   | macOS, Intel | `dotnet publish src/OpenMyo.App -c Release -r osx-x64 -o publish` | `OpenMyo` |

   You can build for either platform from either one.

2. Open the result. On macOS, run it from Terminal (`./publish/OpenMyo`) or double-click it in Finder.

## Things to know

- On macOS the result is a plain executable, not an `.app` bundle.
- The builds aren't code-signed. Windows SmartScreen and macOS Gatekeeper warn the first time you open a copy downloaded from elsewhere.
- The first launch takes a few seconds longer while the app unpacks itself.

## Use the built app with Claude Desktop

Open the built app and choose **Settings** > **Connect to Claude Desktop**. Claude Desktop then launches that same file, so keep it where it is, or connect again after you move it. For details, see [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md).

## Related content

- [Quickstart: Sign in and run OpenMyo](quickstart.md)
- [Use OpenMyo with Claude Desktop](use-with-claude-desktop.md)
- [Data and cache locations](reference/data-locations.md)
