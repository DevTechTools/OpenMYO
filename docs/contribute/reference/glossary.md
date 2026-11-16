---
title: Glossary
description: Definitions of the terms used across the OpenMyo code and documentation, such as card, chapter, media id, MCP, PKCE and trigram.
ms.topic: reference
ms.date: 11/16/2026
---

# Glossary

| Term | Meaning |
|---|---|
| Card | A Yoto playlist, stored by Yoto as one JSON document |
| Chapter | One entry in a card; what the user sees as a track. Usually holds one track |
| MYO | Make Your Own: Yoto's user-created cards |
| Media id | 43-character id of an uploaded image or icon, written as `yoto:#<id>` |
| MCP | Model Context Protocol, how Claude Desktop calls OpenMyo's tools |
| PKCE | Proof Key for Code Exchange, the OAuth flow for apps without a client secret |
| DPAPI | Windows Data Protection API, used to encrypt secrets on Windows |
| Trigram | A three-character slice of a word, used for fuzzy matching |

## Related content

- [Architecture overview](../architecture/overview.md)
- [Card model](../concepts/card-model.md)
- [Icon matching](../concepts/icon-matching.md)
