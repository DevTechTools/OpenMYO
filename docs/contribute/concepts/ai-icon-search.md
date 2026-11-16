---
title: AI icon search
description: Understand how the optional Search with AI button asks Claude for drawable keywords that OpenMyo then matches locally.
ms.topic: concept-article
ms.date: 11/16/2026
---

# AI icon search

**Search with AI** is the optional, click-only way to find icons with Claude. Claude never sees icon ids and
never chooses an icon. It only suggests a few *drawable keywords* for a title, which are then matched locally
like any other search (see [Icon matching](icon-matching.md)).

- [`AiIconSearch`](../../../src/OpenMyo.Core/Icons/AiIconSearch.cs) runs only when the user clicks the button in
  the icon picker, and caches answers per title for the session.
- The prompt includes the index vocabulary, so Claude picks words that actually exist on icons. The system
  prompt is prompt-cached and the output is constrained to a JSON schema. The implementation is in `src/OpenMyo.Claude/`.
- The model comes from Settings. The API key comes from `ISecretStore`.
- If there is no key, or the call fails, matching carries on with trigrams only.

## Related content

- [Icon matching](icon-matching.md)
- [Community icons](community-icons.md)
- [Match icons](../../user/match-icons.md)
