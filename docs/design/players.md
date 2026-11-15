---
description: "Explanation of track search and playing on Yoto players: how search matches, the output picker, MQTT control, connection lifecycle and safety defaults; read when changing search or player control."
---

# Track search and Yoto players

## Searching tracks

The library search field matches playlist titles and track titles together. The library already loads every
playlist's chapters in the background (from the `updatedAt` card cache), so search makes no extra requests.

- `SearchQuery` splits the text into words. A title matches when it contains every word, ignoring case and
  accents (`CompareOptions.IgnoreCase | IgnoreNonSpace`), and returns the matched ranges for highlighting.
- `TrackSearch.Match` checks the playlist title and each chapter title. A chapter is a track to the user,
  so the 1-based chapter position is the track number shown.
- Playlists whose title matches come first, then those that match only on tracks. A row lists up to three
  matching tracks. Clicking one opens the playlist with that track selected and scrolled into view.
- Searching while details are still loading inserts newly matching rows in place, rather than rebuilding the
  list, so the playlist open in the editor keeps its unsaved edits. The summary line says how many playlists
  are still being checked, so an incomplete result isn't mistaken for "no results".
- The MCP tool `search_tracks` uses the same matching.

## Playing on a Yoto player

The output button sits in the lower-right of the now-playing bar. Its menu lists **This computer** and every
player on the account; offline players are dimmed. It works like choosing a speaker on a phone:

- **Changing the output never changes what a player is doing.** If the app itself is playing, that moves
  to the chosen player at the same track and position, but only when the player is idle. A player that is
  already playing or paused carries on, and the bar shows it with a notice. Each ▶ after that plays on that
  player.
- **Players already playing show in the bar** from sign-in, one line each ("Kitchen Yoto · The Gruffalo ·
  1:23 / 4:00") with pause/resume, volume and **Control**. Control makes that player the output without
  touching what it plays. ▶ on a track still plays on the chosen output. (`LivePlayersViewModel`)
- Seeking happens only when the slider is let go (or an arrow key released). The slider also moves by itself
  when a new track length arrives, and seeking is a `card/start`, so treating that as a seek restarted the
  track in a loop.
- While a player is chosen, the bar stays visible, names the player and shows its live state, so it is
  always clear where audio goes.
- **The choice is not saved.** Every launch starts on this computer, so opening the app never plays out loud
  in another room unexpectedly.
- Files being imported exist only on this computer, so they always play locally (the bar says so).
- If the player stops answering, the app tries once more, then falls back to this computer with a notice.

### Protocol

| What | How |
|---|---|
| List players | `GET /device-v2/devices/mine` (`family:devices:view`) |
| Commands | MQTT publish to `device/{id}/command/card/start`, `card/pause`, `card/resume`, `card/stop`, `volume/set` |
| State | Subscribe to `device/{id}/data/events`. A message may carry only the fields that changed, so `PlayerState.Merge` folds each one into the previous state. |
| Broker | `wss://aqrphjqbp3u2z-ats.iot.eu-west-2.amazonaws.com/mqtt`, MQTT 3.1.1 over WebSockets |
| Auth | Username `<deviceId>?x-amz-customauthorizer-name=PublicJWTAuthorizer`, password = the access token (`family:devices:control`), client id `DASH<deviceId>` |

Players report volume in steps (0–16) as `volume`, and their parental limit as `volumeMax`; the bar's slider
works in steps and stops at that limit. `volume/set` takes a percentage instead, so `PlayerMessages`
converts, rounding up so the player lands on the step asked for (step 10 is sent as 63%).

`YotoPlayerRemote` turns off MQTTnet's packet fragmentation. AWS IoT closes the connection when an MQTT
packet arrives split across WebSocket frames. Subscribing still works with fragmentation on, so the fault
only shows when the first command is sent.

`card/start` addresses the card by its short link, `https://yoto.io/<cardId>`, plus `chapterKey`,
`trackKey` and `secondsIn`. Yoto has no "next" command, so next, previous and seeking are `card/start`
again with the neighbouring chapter or a new `secondsIn` (`PlayTargets`).

### Connection lifecycle

`YotoPlayerRemote` holds one shared `PlayerConnection` per player; the bar, the live list and commands all use
it:

- From sign-in the app connects to every online player (re-listing every 2 minutes) so it can show ones
  already playing. Watching only listens. Signing out closes every connection.
- Switching the output away from a player does not disconnect it; the live list keeps watching it.
- Yoto closes connections that are quiet for about five minutes, so the remote publishes `events/request`
  every 295 s. That also asks the player for fresh state.
- The broker checks the token only when connecting. A dropped connection is reopened with a fresh token on
  the next command. After an unexpected drop the connection waits 3 s and tries once more, then reports the
  player as gone (the bar falls back to this computer; the live list drops its line).
- The client id is fixed per player, so only one connection per player can be open at a time. The Claude
  Desktop tools run in a separate process and disconnect right after each command. That keeps any clash
  with the app brief, and the app's reconnect recovers from it.

Players report changes rather than a steady clock, so the bar ticks the position forward locally between
reports.
