# Contributing to OpenMyo

Thanks for helping. Please read [CODESTANDARDS.md](CODESTANDARDS.md) first; it is the source of
truth for review. AI agents should also read [CLAUDE.md](CLAUDE.md).

## Setup

1. Install the .NET 10 SDK (see `global.json`).
2. Clone the repository and run `dotnet build OpenMyo.sln`.
3. To run the app, register your own Yoto application as described in the [README](README.md) and
   paste the client id into Settings.

## Start here

- **New to the code:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), the system, code map and invariants.
- **Adding a page or panel:** [src/OpenMyo.App/README.md](src/OpenMyo.App/README.md), then
  [How to add a page to the sidebar](docs/how-to.md#how-to-add-a-page-to-the-sidebar).
- **Adding a Yoto API call or card field:** [docs/how-to.md](docs/how-to.md#how-to-call-a-new-yoto-endpoint).
- **Adding a setting or secret:** [docs/how-to.md](docs/how-to.md#how-to-add-a-setting-or-a-secret).
- **Adding a Claude Desktop tool:** [docs/how-to.md](docs/how-to.md#how-to-add-a-claude-desktop-tool).
- **Icon matches look wrong:** [How to improve icon matches](docs/how-to.md#how-to-improve-icon-matches),
  background in [docs/design/icon-matching.md](docs/design/icon-matching.md).
- **Changing import, sign-in or saving:** the matching explanation in [docs/design/](docs/design).

## Workflow

1. Open an issue for anything non-trivial so the approach can be discussed first.
2. Branch from `main`. Keep the change to one logical unit.
3. Write the code and tests together. A bug fix includes a test that fails without it.
4. Run the checks below, then open a pull request using the template.

## Running checks

```bash
dotnet build OpenMyo.sln      # warnings are errors
dotnet test OpenMyo.sln
bash eng/check-file-sizes.sh
```

CI runs the same steps on macOS and Windows and must pass before merge.

## Pull request checklist

- Dependency rules hold; Core references nothing else in the solution (section 1)
- Files, methods, parameters and nesting are within the hard limits (section 2)
- Design principles followed: single responsibility, immutable records, no static mutable state
  (section 3)
- C# style and naming followed; no suppressions without a reason (section 4)
- I/O is async with a trailing `CancellationToken`; bounded parallelism (section 5)
- Efficiency: streaming, caching and typed `HttpClient`s (section 6)
- No secrets in files, logs or URLs; external input validated (section 7)
- UI follows MVVM, compiled bindings, Discord-style design design, keyboard and accessibility rules (section 8)
- Tests follow `Method_Scenario_ExpectedResult` and never touch the network or real keychains
  (section 9)
- Public types have useful XML docs; no commented-out code or unlinked TODOs (section 10)
- New packages are justified and versioned only in `Directory.Packages.props` (section 11)
- Docs updated when behaviour or rules change (section 12)

## Commit style

Small, focused commits in imperative mood, for example `Add trigram icon index`. One logical change
per pull request.

## Licensing

By submitting a contribution you agree that it is licensed under the [Apache License 2.0](LICENSE), as
section 5 of the license describes.

## Reporting security issues

Do not file public issues for vulnerabilities. See [SECURITY.md](SECURITY.md).
