# OpenMyo

An unofficial, open-source desktop app for managing Yoto "Make Your Own" (MYO) playlists on macOS and
Windows. It imports folders of audio as playlists, cleans up track titles and matches icons. It
searches every playlist for a track by title and can play any track on one of your Yoto players. Claude
Desktop can also use it to rename tracks, pick icons, find tracks and play them on a player. Written in C# with .NET 10 and Avalonia 11.

OpenMyo is not affiliated with or endorsed by Yoto. Each user signs in with their own Yoto account and
their own registered Yoto app.

For what the app does and how to use it, see the [user guide](docs/USER_GUIDE.md).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (exact version pinned in `global.json`)
- macOS or Windows
- A Yoto account

## Run it

1. Register an app at <https://yoto.dev> as a **public client** with the redirect URI
   `http://127.0.0.1:8787/callback`, and copy its client id.
2. Start the app:

   ```bash
   dotnet run --project src/OpenMyo.App
   ```

3. Open **Settings**, paste the client id and sign in.

## Build the app

There are no prebuilt downloads yet, so build the app yourself. Each command below makes one file that
runs without .NET installed, in `publish/`:

| Platform | Command | Output |
|---|---|---|
| Windows | `dotnet publish src/OpenMyo.App -c Release -r win-x64 -o publish` | `OpenMyo.exe` |
| macOS, Apple silicon | `dotnet publish src/OpenMyo.App -c Release -r osx-arm64 -o publish` | `OpenMyo` |
| macOS, Intel | `dotnet publish src/OpenMyo.App -c Release -r osx-x64 -o publish` | `OpenMyo` |

You can build for either platform from either one. Things to know:

- On macOS the result is a plain executable, not an `.app` bundle. Run it from Terminal
  (`./publish/OpenMyo`) or double-click it in Finder.
- The builds aren't code-signed. Windows SmartScreen and macOS Gatekeeper will warn the first time a
  copy downloaded from elsewhere is opened.
- The first launch takes a few seconds longer while the app unpacks itself.

To use it with Claude Desktop, open the built app and choose **Settings → Connect to Claude Desktop**.
Claude Desktop then launches that same file, so keep it where it is or connect again after moving it.

## Build and test

```bash
dotnet build OpenMyo.sln          # warnings are errors
dotnet test OpenMyo.sln
bash eng/check-file-sizes.sh      # file size limits from CODESTANDARDS.md
```

CI runs the same three steps on macOS and Windows.

## Project layout

```
src/
  OpenMyo.Core/            Domain logic: playlists, icons, import, publishing, library
  OpenMyo.Yoto/            Yoto API client, OAuth, JSON mapping
  OpenMyo.Claude/          Optional Claude icon keyword suggestions
  OpenMyo.Mcp/             MCP server for Claude Desktop
  OpenMyo.Infrastructure/  Audio tags, Keychain/DPAPI, settings, paths
  OpenMyo.App/             Avalonia UI and composition root
tests/                     One test project per production project with logic
```

[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) explains how these fit together.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) for the workflow and [CODESTANDARDS.md](CODESTANDARDS.md) for the
rules every change is reviewed against. Open an issue before starting anything large. Report security
problems privately as described in [SECURITY.md](SECURITY.md).

## License

[Apache License 2.0](LICENSE). See [NOTICE](NOTICE) for attribution.
