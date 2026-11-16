---
title: Use OpenMyo with Claude Desktop
description: Connect OpenMyo to Claude Desktop so Claude can rename playlists and tracks, pick icons, find tracks and play them.
ms.topic: how-to
ms.date: 11/16/2026
---

# Use OpenMyo with Claude Desktop

OpenMyo can act as an [MCP](https://modelcontextprotocol.io) server, so Claude Desktop can rename playlists and tracks, pick icons, find tracks across your playlists and play them on a Yoto player ("play the Gruffalo on the bedroom Yoto"). It uses your own Claude subscription. You don't need an API key.

## Prerequisites

- Claude Desktop.
- OpenMyo, signed in to Yoto. The server uses the Yoto session you already signed in with, so sign in first.

## Connect

1. In OpenMyo, open **Settings**.
2. Choose **Connect to Claude Desktop**. This adds OpenMyo to Claude Desktop's configuration.
3. Restart Claude Desktop.

Claude Desktop starts OpenMyo in the background when it needs it; you don't have to keep the app open. Claude can list and read your playlists, rename playlists and tracks, search and set icons, find tracks, and play them on your Yoto players.

> [!TIP]
> If you move the app, choose **Connect to Claude Desktop** again. To remove the connection, choose **Disconnect**.

## Try some prompts

- "Pick icons for my Odyssey playlist."
- "Which tracks in Bedtime Stories have no icon? Find good ones for them."
- "Number the icons on my Counting Songs playlist."
- "Remove the 'Chapter N -' prefix from every track in The Hobbit."
- "Rename my Bedtime playlist to Bedtime Stories."

## Undo a change

Changes save straight to Yoto. Claude Desktop asks before each tool call, and each change reports the previous titles or icons, so you can ask Claude to undo it.

## Related content

- [Build the app](build-the-app.md)
- [Claude Desktop connector](../contribute/concepts/claude-desktop-connector.md)
