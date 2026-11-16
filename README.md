# OpenMyo

An unofficial, open-source desktop app for managing Yoto "Make Your Own" (MYO) playlists on macOS and
Windows. It imports folders of audio and podcast feeds as playlists, cleans up track titles and matches
icons. It searches every playlist for a track by title and can play any track on this computer or on one
of your Yoto players. Claude Desktop can also use it to rename tracks, pick icons, find tracks and play
them on a player. Written in C# with .NET 10 and Avalonia 11.

> [!NOTE]
> OpenMyo isn't affiliated with or endorsed by Yoto. Each user signs in with their own Yoto account and
> their own registered Yoto app.

## Documentation

All documentation is in [docs/](docs/index.md):

- **Use OpenMyo:** [What is OpenMyo?](docs/user/overview.md),
  [Quickstart: Sign in and run OpenMyo](docs/user/quickstart.md),
  [Build the app](docs/user/build-the-app.md), and one guide per feature.
- **Contribute:** [Architecture overview](docs/contribute/architecture/overview.md),
  [concepts](docs/contribute/concepts), [how-to guides](docs/contribute/how-to) and
  [reference](docs/contribute/reference).

## Get started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) (exact version pinned in
`global.json`), macOS or Windows, and a Yoto account.

```bash
dotnet run --project src/OpenMyo.App
```

Before you can sign in, register your own Yoto app. Follow
[Quickstart: Sign in and run OpenMyo](docs/user/quickstart.md). There are no prebuilt downloads yet;
to make a standalone executable, see [Build the app](docs/user/build-the-app.md).

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
  OpenMyo.Yoto/            Yoto API client, OAuth, JSON mapping, player control
  OpenMyo.Claude/          Optional Claude icon keyword suggestions
  OpenMyo.Mcp/             MCP server for Claude Desktop
  OpenMyo.Infrastructure/  Audio tags and playback, Keychain/DPAPI, settings, paths
  OpenMyo.App/             Avalonia UI and composition root
tests/                     One test project per production project with logic
tools/OpenMyo.Snapshots/   Renders screens to PNG without signing in
```

The [architecture overview](docs/contribute/architecture/overview.md#projects) explains how these fit together.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) for the workflow and [CODESTANDARDS.md](CODESTANDARDS.md) for the
rules every change is reviewed against. Open an issue before starting anything large. Report security
problems privately as described in [SECURITY.md](SECURITY.md).

## License

[Apache License 2.0](LICENSE). See [NOTICE](NOTICE) for attribution.
