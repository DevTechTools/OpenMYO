---
title: Design decisions
description: Review the main design decisions in OpenMyo, why each was made, and what it gives users, before you reverse or revisit one.
ms.topic: reference
ms.date: 11/16/2026
---

# Design decisions

Read this table before you reverse or revisit a decision.

| Decision | Why | What the user gets |
|---|---|---|
| Avalonia, not MAUI | MAUI has no real macOS desktop target (it goes through Mac Catalyst). Avalonia runs one codebase as a normal desktop app on macOS and Windows. | The same app, with the same features, on a Mac or a Windows PC. |
| PKCE, not device code | Yoto has deprecated the device code flow. Authorization Code + PKCE with a loopback redirect is the supported option for desktop apps. | Signs in on Yoto's own page in their browser; OpenMyo never sees their password. |
| Each user registers their own Yoto client | No client secret can be shipped safely in an open-source binary, and there is no OpenMyo server. | Their data goes straight between their computer and Yoto, through nobody else. |
| Patch card JSON, don't rebuild it | Saving replaces the whole card, so a rebuild from domain records would delete fields OpenMyo doesn't model (see [Read and save cards](../concepts/read-and-save-cards.md)). | Editing a card in OpenMyo never breaks settings made in the Yoto app. |
| Cache cards by `updatedAt` | Unchanged cards need no request (see [Read and save cards](../concepts/read-and-save-cards.md)). | A large library opens in seconds after the first time. |
| Claude returns keywords, not icon ids | The icon catalog never goes in the prompt. Keywords are matched locally, so everything works without Claude. | AI is optional; without an API key icon matching still works. |
| Keep Claude calls small | Claude only runs on a click, answers are cached per title, the vocabulary is capped and prompt-cached, effort is low, and the token limit scales with the request. | Using their own API key costs very little, and nothing unless they click. |
| Trigram matching by default | Fast, free, deterministic and easy to test. | Icons for a whole playlist are suggested instantly, and the same titles always get the same icons. |
| Harvest yotoicons.com instead of querying it | Searches stay instant and offline, and a free hobby site gets very few requests instead of one per keystroke. | Many more icons to choose from, with no waiting while typing. |
| Strict layering | Core has no UI, HTTP or platform code, so matching, ordering and cleanup stay pure and unit-tested. | Fewer regressions: the rules that shape their playlists are covered by tests. |
| MQTT for Yoto player control | Yoto accepts play, pause and volume commands only over its MQTT broker (see [Yoto player control](../concepts/yoto-player-control.md#protocol)). MQTTnet is the one extra dependency. | Any track can be sent to a Yoto player in the house, with live controls. |
| Output resets to this computer on every launch | A remembered player would make the first ▶ after opening the app play out loud in another room. | Opening OpenMyo never wakes a sleeping child. |
| One search field for playlists and tracks | One place to look, as the search field already filters the library; track details are already loaded for the health filters. | Type a song name and see which playlists have it, with the matching words highlighted. |

## Related content

- [Architecture overview](../architecture/overview.md)
- [Invariants](../architecture/invariants.md)
- [Track search](../concepts/track-search.md)
