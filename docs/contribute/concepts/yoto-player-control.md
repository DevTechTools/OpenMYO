---
title: Yoto player control
description: Understand how OpenMyo plays audio on a Yoto player over MQTT, including the output picker, protocol, connection lifecycle and safety defaults.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Yoto player control

## Playing on a Yoto player

The output button in the now-playing bar lists **This computer** and every player on the account. It works
like choosing a speaker on a phone:

- **Changing the output never changes what a player is doing.** If the app itself is playing, that moves
  to the chosen player at the same track and position, but only when the player is idle. A player that is
  already playing or paused carries on, and the bar shows it with a notice.
- **Players already playing show in the bar** from sign-in. **Control** makes that player the output without
  touching what it plays.
- Seeking happens only when the slider is let go. The slider also moves by itself when a new track length
  arrives, and seeking is a `card/start`, so treating that as a seek restarted the track in a loop.
- While a player is chosen, the bar stays visible and names the player, so it is always clear where audio goes.
- **The choice is not saved.** Every launch starts on this computer, so opening the app never plays out loud
  in another room unexpectedly.
- Files being imported exist only on this computer, so they always play locally.
- If the player stops answering, the app tries once more, then falls back to this computer with a notice.

## Protocol

| What | How |
|---|---|
| List players | `GET /device-v2/devices/mine` (`family:devices:view`) |
| Commands | MQTT publish to `device/{id}/command/card/start`, `card/pause`, `card/resume`, `card/stop`, `volume/set` |
| State | Subscribe to `device/{id}/data/events`. A message may carry only the fields that changed, so each one is merged into the previous state. |
| Broker | Yoto's AWS IoT endpoint, MQTT 3.1.1 over WebSockets |
| Auth | Username `<deviceId>?x-amz-customauthorizer-name=PublicJWTAuthorizer`, password = the access token (`family:devices:control`), client id `DASH<deviceId>` |

Players report volume in steps and their parental limit as `volumeMax`; the bar's slider works in steps and
stops at that limit. `volume/set` takes a percentage instead, so the app converts, rounding up so the player
lands on the step asked for.

[`YotoPlayerRemote`](../../../src/OpenMyo.Yoto/Devices/YotoPlayerRemote.cs) turns off MQTTnet's packet
fragmentation. AWS IoT closes the connection when an MQTT packet arrives split across WebSocket frames.
Subscribing still works with fragmentation on, so the fault only shows when the first command is sent.

`card/start` addresses the card by its short link, `https://yoto.io/<cardId>`, plus `chapterKey`, `trackKey`
and `secondsIn`. Yoto has no "next" command, so next, previous and seeking are `card/start` again with the
neighbouring chapter or a new `secondsIn`.

## Connection lifecycle

`YotoPlayerRemote` holds one shared connection per player; the bar, the live list and commands all use it:

- From sign-in the app connects to every online player so it can show ones already playing. Watching only
  listens. Signing out closes every connection.
- Switching the output away from a player does not disconnect it; the live list keeps watching it.
- Yoto closes connections that are quiet for about five minutes, so the remote publishes `events/request`
  just under that. That also asks the player for fresh state.
- The broker checks the token only when connecting. A dropped connection is reopened with a fresh token on
  the next command. After an unexpected drop the connection waits briefly and tries once more, then reports
  the player as gone (the bar falls back to this computer; the live list drops its line).
- The client id is fixed per player, so only one connection per player can be open at a time. The Claude
  Desktop tools run in a separate process and disconnect right after each command. That keeps any clash
  with the app brief, and the app's reconnect recovers from it.

Players report changes rather than a steady clock, so the bar ticks the position forward locally between
reports.

## Related content

- [Track search](track-search.md)
- [Sign-in and tokens](sign-in-and-tokens.md)
- [Claude Desktop connector](claude-desktop-connector.md)
- [Play audio](../../user/play-audio.md)
