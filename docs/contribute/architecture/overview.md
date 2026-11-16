---
title: Architecture overview
description: Understand what OpenMyo is, how its desktop app and Claude Desktop connector fit together, and which external systems it talks to.
ms.topic: overview
ms.date: 11/16/2026
---

# Architecture overview

This article gives you the bird's-eye view of OpenMyo: what it does, which systems it talks to, and how the executable is split into containers. Read it when you start work in the repo or need to decide where a change belongs.

## Bird's eye view

OpenMyo is a desktop app for macOS and Windows that manages Yoto "Make Your Own" (MYO) playlists. It
turns folders of audio files into playlists on the user's Yoto account, and tidies existing playlists:
titles, track order, covers and the 16×16 pixel icon shown for each track.

OpenMyo has no server and keeps no playlists of its own. Every playlist lives on Yoto as a JSON document
called a **card**. OpenMyo reads cards, changes them, and saves them back through Yoto's public API. Local
files are settings and caches only.

Most of the interesting logic is local and offline: grouping and ordering files, cleaning titles, and
matching each title to an icon with trigram similarity. Claude is an optional extra in two places: the
user can ask Claude for icon keywords in the icon picker, and Claude Desktop can drive OpenMyo through
the Model Context Protocol (MCP).

## System context

```mermaid
---
title: System context diagram for OpenMyo
---
flowchart TB
    accTitle: System context diagram for OpenMyo
    accDescr: A card maker uses OpenMyo directly or through Claude Desktop. OpenMyo talks to the Yoto platform, and optionally to yotoicons.com and the Anthropic API.
    user["<b>Card maker</b><br/>[Person]<br/>A parent or teacher who makes<br/>Yoto MYO cards"]:::person
    claudeDesktop["<b>Claude Desktop</b><br/>[Software system, external, optional]<br/>Chat app that can call<br/>OpenMyo's tools"]:::ext
    openmyo["<b>OpenMyo</b><br/>[Software system]<br/>Bulk-creates and tidies<br/>MYO playlists"]:::system
    yoto["<b>Yoto platform</b><br/>[Software system, external]<br/>Sign-in, card storage, audio<br/>transcoding, icon library"]:::ext
    yotoicons["<b>yotoicons.com</b><br/>[Software system, external, optional]<br/>Community icon library"]:::ext
    anthropic["<b>Anthropic API</b><br/>[Software system, external, optional]<br/>Claude models"]:::ext

    user -->|"Imports audio folders,<br/>edits playlists, picks icons"| openmyo
    user -->|"Asks for title and<br/>icon changes"| claudeDesktop
    claudeDesktop -->|"Calls tools to read<br/>and change playlists"| openmyo
    openmyo -->|"Signs in; reads and saves cards;<br/>uploads audio and images"| yoto
    openmyo -->|"Downloads the icon listing;<br/>imports chosen icons"| yotoicons
    openmyo -->|"Asks for icon keywords<br/>for one track title"| anthropic

    subgraph key["Key"]
        direction LR
        k1["Person"]:::person
        k2["OpenMyo"]:::system
        k3["External system"]:::ext
    end

    classDef person fill:#08427b,stroke:#052e56,color:#fff
    classDef system fill:#1168bd,stroke:#0b4884,color:#fff
    classDef ext fill:#999999,stroke:#6b6b6b,color:#fff
```

## Containers

```mermaid
---
title: Container diagram for OpenMyo
---
flowchart LR
    accTitle: Container diagram for OpenMyo
    accDescr: The desktop app and the windowless Claude Desktop connector are one executable. Both share the OS secret store, the data and cache folders, and call Yoto, yotoicons.com and, from the app only, Anthropic.
    user["<b>Card maker</b><br/>[Person]"]:::person
    claudeDesktop["<b>Claude Desktop</b><br/>[Software system, external]"]:::ext

    subgraph openmyo["OpenMyo"]
        app["<b>Desktop app</b><br/>[Container: .NET 10, Avalonia]<br/>Library, editor, import<br/>and settings screens"]:::container
        connector["<b>Claude Desktop connector</b><br/>[Container: .NET 10, same executable<br/>started with --mcp]<br/>MCP server, no window"]:::container
        data[("<b>Data folder</b><br/>[Container: files]<br/>settings.json, token lock file,<br/>encrypted secrets on Windows")]:::container
        cache[("<b>Cache folder</b><br/>[Container: files]<br/>Card JSON, icon lists,<br/>yotoicons catalog, images")]:::container
    end

    subgraph computer["User's computer"]
        files["<b>Audio files</b><br/>[Folders on disk]"]:::ext
        browser["<b>System browser</b><br/>[Software system, external]"]:::ext
        secrets["<b>OS secret store</b><br/>[Software system, external]<br/>macOS Keychain or<br/>Windows DPAPI"]:::ext
    end

    subgraph internet["Internet"]
        yoto["<b>Yoto platform</b><br/>[Software system, external]<br/>login.yotoplay.com, api.yotoplay.com,<br/>upload storage"]:::ext
        yotoicons["<b>yotoicons.com</b><br/>[Software system, external]"]:::ext
        anthropic["<b>Anthropic API</b><br/>[Software system, external]"]:::ext
    end

    user -->|"Imports, edits,<br/>picks icons"| app
    claudeDesktop -->|"Starts process; calls tools<br/>[JSON-RPC over stdio]"| connector
    app -->|"Reads tags, artwork<br/>and audio"| files
    app -->|"Opens Yoto sign-in page;<br/>receives redirect<br/>[HTTP, 127.0.0.1:8787]"| browser
    app & connector -->|"Reads and writes<br/>refresh token, API key"| secrets
    app & connector -->|"Reads settings;<br/>takes token lock"| data
    app & connector -->|"Reads and writes<br/>cached data"| cache
    app & connector -->|"Tokens, cards, media, icons<br/>[HTTPS, JSON]"| yoto
    app & connector -->|"Listing pages, icon PNGs<br/>[HTTPS]"| yotoicons
    app -->|"Icon keyword request<br/>[HTTPS, JSON]"| anthropic

    subgraph key["Key"]
        direction LR
        k1["Person"]:::person
        k2["Container"]:::container
        k3[("Data store")]:::container
        k4["External system"]:::ext
    end

    classDef person fill:#08427b,stroke:#052e56,color:#fff
    classDef container fill:#438dd5,stroke:#2e6295,color:#fff
    classDef ext fill:#999999,stroke:#6b6b6b,color:#fff
```

The desktop app and the connector are the same executable ([`Program`](../../../src/OpenMyo.App/Program.cs)
chooses the mode from the `--mcp` argument) and build the same services. They can run at the same time.
They never talk to each other directly; they share the secret store, the data and cache folders, and a
lock file that stops both refreshing the Yoto token at once.

All communication is request and response. The one slow step is on Yoto's side: after an upload, Yoto
transcodes the audio in the background and OpenMyo polls until it is done.

## Projects

The solution is layered. `OpenMyo.Core` holds the domain and the algorithms; every other project either
implements a Core interface or uses Core.

```text
App ──► Core, Yoto, Claude, Mcp, Infrastructure
Yoto, Claude, Mcp, Infrastructure ──► Core
Core ──► nothing in the solution
```

Folders are organised by feature, not by type, and a feature keeps the same folder name in every project
that has it. The App groups its folders by screen instead.

| Project | Owns |
|---|---|
| [`OpenMyo.Core`](../../../src/OpenMyo.Core) | Domain records, the interfaces other projects implement, import, icon matching and other pure logic |
| [`OpenMyo.Yoto`](../../../src/OpenMyo.Yoto) | Everything that talks to Yoto and yotoicons.com: sign-in, HTTP pipeline, cards, media, icons, player control |
| [`OpenMyo.Claude`](../../../src/OpenMyo.Claude) | The Anthropic API calls and their prompts |
| [`OpenMyo.Mcp`](../../../src/OpenMyo.Mcp) | The Claude Desktop connector's MCP server and tools |
| [`OpenMyo.Infrastructure`](../../../src/OpenMyo.Infrastructure) | Local-machine implementations: audio, secret stores, settings, paths, feeds |
| [`OpenMyo.App`](../../../src/OpenMyo.App) | The Avalonia UI and the composition root; see its [README](../../../src/OpenMyo.App/README.md) |

[`tests/`](../../../tests) has one project per library, mirroring its folders.
[`tools/OpenMyo.Snapshots`](../../../tools/OpenMyo.Snapshots) renders screens to PNG with sample data.

For the data and cache folder locations, see [Data locations](../../user/reference/data-locations.md).

## Related content

- [Invariants](invariants.md)
- [Cross-cutting concerns](cross-cutting-concerns.md)
- [Design decisions](../reference/design-decisions.md)
- [Glossary](../reference/glossary.md)
