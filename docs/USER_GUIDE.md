# OpenMyo user guide

How to use OpenMyo once it is running. To build and run it, see the [README](../README.md).

## What it does

Bulk import. Choose or drag in folders or audio files and each folder becomes a playlist. Track
numbers, underscores, "(Official Audio)"-style noise and repeated artist prefixes are removed from
titles. Tracks are ordered by disc and track tags, then leading numbers, then name. Cover art comes from
an image in the folder (`cover.jpg`, `folder.jpg`, ...) or from art embedded in the audio, and a track
gets an icon from an image with the same base name as its audio file.

Import from a podcast feed. On the Import screen choose **From Feed…** (or paste a feed link anywhere on
the screen), then **Preview** to list the episodes. The latest 100 are ticked, since a playlist holds up
to 100 tracks. Narrow the list with **Length** (from / to, in minutes), **Exclude titles with** (for
example `trailer, bonus`) and **Only titles with**; hidden episodes are never downloaded. **Order** sets
oldest or newest first, both in the list and on the card. Change the ticks, then **Download & Add**. The
episodes download into a temporary folder and appear as a draft named after the show, with the show's
artwork as its cover. Review it like any other import and choose **Create Playlists**. The
downloaded files are deleted once the playlist is created or the draft is discarded.

Library. Your playlists as a list or a grid of covers, with filters for empty playlists, missing icons
and missing covers. Select several to match their icons or delete them together.

Search. The library's search field finds playlists by title and tracks by title, across every playlist.
Every word you type must appear, in any order, ignoring capitals and accents ("gruff child" finds "The
Gruffalo's Child"). Matching tracks are listed under their playlist with the matched words highlighted;
click one to open the playlist at that track, ready to play.

Playlist editor. Rename tracks, drag to reorder, set the cover and pick icons. Hints such as "31 missing
icons" or "Tidy 8 titles" only appear when they apply, and each is one click. Select several tracks to
give them the same icon, number them or remove them.

Playback. Hover a track in the playlist editor or on the Import screen and press ▶ to listen from that
track on; the rest of the playlist plays after it. The bar along the bottom of the window has play/pause,
previous, next, a position slider and volume, and keeps playing while you move between screens. The
keyboard's media keys and Cmd/Ctrl+Shift+Space also play and pause. Import previews play your local files
before anything is uploaded.

Play on a Yoto player. While something plays, the speaker button in the bottom-right corner of the bar
lists **This computer** and your Yoto players (offline ones are greyed out). Choose an idle player and
what is playing here moves to it, at the same track and position; a player that is already playing
carries on undisturbed. Yoto players that are playing also show as a line each in the bar, with pause,
volume and **Control** (which makes that player the output without changing what it plays). Until you
switch back, every ▶ plays on that player,
and the bar shows its track, play/pause, next/previous, position and volume. OpenMyo always starts on
**This computer**, so opening it never plays out loud in another room. The first time after this feature
arrives, Yoto asks you to sign in once more to allow OpenMyo to control your players.

Title clean-up. Each cleanup rule can be switched on or off, you can add your own find and replace, and
a preview shows the result before anything changes. Titles that follow a pattern on purpose, like
"[051] Episode", are left alone.

Icon matching. Track titles are matched by trigram similarity against Yoto's public icons, the icons
already in your playlists and Yoto's number icons. If Yoto has nothing close, a strong match from
[yotoicons.com](https://yotoicons.com) is added to your icons. This doesn't use AI.

Community icons. The icon picker also searches yotoicons.com, credits the artist and adds the icon you
choose to your Yoto account. The search runs against a local copy of the catalog (about 26,000 icons),
refreshed weekly. Images are only downloaded when shown.

Search with AI (optional). With your own Anthropic API key, each icon picker gets a **Search with AI**
button. Claude only runs when you click it. It suggests a few words for that one title, and those are
matched against the local icon index. Answers are cached per title.

Rename with AI (optional). In the playlist editor's **⋯** menu, **Rename Every Title with AI** asks Claude
for a clean title for every track (no track numbers, file-name leftovers or repeated album names). The new
titles show as unsaved edits: check them, then **Save**, or **Revert** to undo. Without an API key, a
connected Claude Desktop gets the request instead and saves straight to Yoto.

Secrets are stored in the macOS Keychain or Windows DPAPI, never in settings files.

## Claude setup (optional)

Icon matching works without Claude. To add **Search with AI** to the icon picker, turn on
**AI Icon Search** in **Settings**, then paste an API key from the Anthropic console. The default model is `claude-opus-5-5` and can be changed there. The
key is stored in the Keychain / DPAPI. Claude never sees your audio, only track titles.

## Use with Claude Desktop

OpenMyo can act as an [MCP](https://modelcontextprotocol.io) server, so Claude Desktop can rename playlists
and tracks, pick icons, find tracks across your playlists and play them on a Yoto player ("play the
Gruffalo on the bedroom Yoto"). It uses your own Claude subscription; no API key is needed.

Claude Desktop launches the OpenMyo app with `--mcp` and talks to it over stdio. The
server uses the Yoto session you already signed in with in OpenMyo, so sign in there first. In
**Settings**, **Connect to Claude Desktop** adds OpenMyo to Claude Desktop's configuration; restart Claude
Desktop afterwards.

Tools: `list_playlists`, `get_playlist`, `rename_playlist`, `rename_tracks`, `search_icons` (Yoto, your uploads and
yotoicons.com), `set_track_icons`, `auto_match_icons` and `number_track_icons`.

Example prompts:

- "Pick icons for my Odyssey playlist."
- "Which tracks in Bedtime Stories have no icon? Find good ones for them."
- "Number the icons on my Counting Songs playlist."
- "Remove the 'Chapter N -' prefix from every track in The Hobbit."
- "Rename my Bedtime playlist to Bedtime Stories."

Changes save straight to Yoto. Claude Desktop asks before each tool call, and each change reports the
previous titles or icons, so you can ask Claude to undo it.
