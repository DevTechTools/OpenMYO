---
title: Data and cache locations
description: Where OpenMyo stores settings, secrets and cached data on macOS and Windows, and which folders are safe to delete.
ms.topic: reference
ms.date: 11/16/2026
---

# Data and cache locations

OpenMyo keeps small settings in a data folder and re-downloadable data in a cache folder.

## Folders

| | macOS | Windows |
|---|---|---|
| Data folder | `~/Library/Application Support/OpenMyo` | `%LOCALAPPDATA%\OpenMyo` |
| Cache folder | `~/Library/Caches/OpenMyo` | `%LOCALAPPDATA%\OpenMyo\Cache` |

## What's in each

| Folder | Item | Contents |
|---|---|---|
| Data | `settings.json` | Your settings. Never secrets. |
| Data | `secrets/*.bin` | Windows only: your secrets, encrypted with DPAPI for your Windows user. |
| Data | `yoto-refresh.lock` | A lock that keeps the app and the Claude Desktop server from refreshing your Yoto sign-in at the same time. |
| Cache | `cards/` | Cached playlist (card) JSON. |
| Cache | `images/` | Downloaded artwork and icon images. |
| Cache | `icons/public.json` | Yoto's public icon catalog. |
| Cache | `yotoicons/catalog.json` | Local copy of the yotoicons.com catalog, updated automatically. |

> [!TIP]
> The cache folder is safe to delete. OpenMyo downloads what it needs again.

## Secrets

Secrets (your Yoto sign-in tokens and Anthropic API key) aren't kept in settings files. On macOS they're in the Keychain. On Windows they're encrypted with DPAPI and stored in the data folder's `secrets` folder; only your Windows user can decrypt them.

## Related content

- [OpenMyo overview](../overview.md)
- [Use OpenMyo with Claude Desktop](../use-with-claude-desktop.md)
- [Cross-cutting concerns: caching](../../contribute/architecture/cross-cutting-concerns.md#caching)
