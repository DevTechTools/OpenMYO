---
title: OpenMyo documentation
description: Learn how to use OpenMyo to import, tidy and play Yoto Make Your Own playlists, and how to contribute to its code.
ms.topic: overview
ms.date: 11/16/2026
---

# OpenMyo documentation

OpenMyo is an unofficial, open-source desktop app for macOS and Windows that manages Yoto "Make Your Own"
(MYO) playlists. These pages are split by audience: start under **Use OpenMyo** if you want to run the
app, or under **Contribute** if you want to change it.

> [!NOTE]
> OpenMyo isn't affiliated with or endorsed by Yoto. Each user signs in with their own Yoto account and
> their own registered Yoto app.

## Use OpenMyo

| Get started | Work with playlists | Go further |
|---|---|---|
| [What is OpenMyo?](user/overview.md) | [Import folders of audio](user/import-folders.md) | [Use OpenMyo with Claude Desktop](user/use-with-claude-desktop.md) |
| [Quickstart: Sign in and run OpenMyo](user/quickstart.md) | [Import a podcast feed](user/import-podcast-feed.md) | [Where OpenMyo stores data](user/reference/data-locations.md) |
| [Build the app](user/build-the-app.md) | [Browse and search your library](user/library-and-search.md) | |
| | [Edit playlists](user/edit-playlists.md) | |
| | [Match icons](user/match-icons.md) | |
| | [Play audio here or on a Yoto player](user/play-audio.md) | |

## Contribute

| Understand the code | Change the code | Look things up |
|---|---|---|
| [Architecture overview](contribute/architecture/overview.md) | [Call a new Yoto endpoint](contribute/how-to/call-yoto-endpoint.md) | [Invariants](contribute/architecture/invariants.md) |
| [Cross-cutting concerns](contribute/architecture/cross-cutting-concerns.md) | [Store a new card field](contribute/how-to/add-card-field.md) | [Design decisions](contribute/reference/design-decisions.md) |
| [Card model](contribute/concepts/card-model.md) | [Add domain logic](contribute/how-to/add-domain-logic.md) | [Glossary](contribute/reference/glossary.md) |
| [Sign-in and tokens](contribute/concepts/sign-in-and-tokens.md) | [Add a setting or a secret](contribute/how-to/add-setting-or-secret.md) | |
| [Import pipeline](contribute/concepts/import-pipeline.md) | [Add a Claude Desktop tool](contribute/how-to/add-claude-desktop-tool.md) | |
| [Icon matching](contribute/concepts/icon-matching.md) | [Add a page to the navigation rail](contribute/how-to/add-navigation-page.md) | |
| [Claude Desktop connector](contribute/concepts/claude-desktop-connector.md) | [Add a panel to a screen](contribute/how-to/add-panel.md) | |
| [Yoto player control](contribute/concepts/yoto-player-control.md) | [Improve icon matches](contribute/how-to/improve-icon-matches.md) | |

Every change follows [CODESTANDARDS.md](../CODESTANDARDS.md) and the workflow in
[CONTRIBUTING.md](../CONTRIBUTING.md). The full list of pages is in [toc.yml](toc.yml).

## Related content

- [OpenMyo on GitHub: README](../README.md)
- [Security policy](../SECURITY.md)
- [Yoto API reference](https://yoto.dev/api/)
