# CLAUDE.md

Guidance for Claude (and other AI coding agents) working in this repository.

## Read first

1. **[CODESTANDARDS.md](CODESTANDARDS.md)** is mandatory. Every change must follow it, especially the
   dependency rules (§1), size limits (§2), async rules (§5) and UI rules (§8).
2. [docs/contribute/architecture/overview.md](docs/contribute/architecture/overview.md): what OpenMyo is
   its containers and [projects](docs/contribute/architecture/overview.md#projects), with the
   [invariants](docs/contribute/architecture/invariants.md) beside it. Details per area are in
   [docs/contribute/concepts/](docs/contribute/concepts); step-by-step guides for common changes are in
   [docs/contribute/how-to/](docs/contribute/how-to). [docs/toc.yml](docs/toc.yml) lists every page.

## What this is

OpenMyo is an unofficial, open-source, cross-platform (macOS + Windows) desktop app for managing
Yoto "Make Your Own" (MYO) playlists. C# / .NET 10 / Avalonia 11, Discord/GitKraken-style dark UI. Key features:

- Bulk import: pick folders or files → titles cleaned, tracks ordered, artwork found → playlists created.
- Library management with smart filters (no tracks, missing icons, missing cover) and bulk actions.
- Icon matching: free trigram matching against Yoto's public icon tags; optional, click-only "Search with AI" (Claude) in the icon picker, enabled in Settings.

## Commands

```bash
dotnet build OpenMyo.sln          # warnings are errors
dotnet test OpenMyo.sln
dotnet run --project src/OpenMyo.App
eng/check-file-sizes.sh           # enforce CODESTANDARDS §2 file limits
```

## Where things go

| Change | Location |
|---|---|
| Domain model, pure logic, new interface | `src/OpenMyo.Core/<Feature>/` |
| Yoto HTTP calls, JSON mapping, OAuth | `src/OpenMyo.Yoto/<Feature>/` |
| Claude prompt / API call | `src/OpenMyo.Claude/` |
| Keychain/DPAPI, tags, settings, paths | `src/OpenMyo.Infrastructure/<Feature>/` |
| Screens and view models, styles | `src/OpenMyo.App/Screens/<Feature>/` (view beside view model), `src/OpenMyo.App/Styles/`; see `docs/contribute/concepts/navigation-and-commands.md` and `docs/contribute/how-to/` |
| Service registration | each project's `ServiceCollectionExtensions.cs`; wired in `src/OpenMyo.App/Composition/` |

## Rules of thumb for agents

- Before adding a class, search for an existing one that owns the responsibility.
- Keep files under 300 lines; split by responsibility when approaching 500 (exceptions: CODESTANDARDS §2).
- Never put secrets in settings, logs, or source. Use `ISecretStore`.
- Never edit `Directory.Packages.props`, `Directory.Build.props` or `.editorconfig` casually; these
  are shared policy. Explain why in the PR when you must.
- Preserve unknown fields when updating Yoto card JSON (patch the original document; never rebuild it
  from a narrow DTO).
- Run `dotnet build` and `dotnet test` before declaring work done.
- Yoto API reference: <https://yoto.dev/api/>. Claude API: default model `claude-opus-5-5`
  (configurable in Settings).
