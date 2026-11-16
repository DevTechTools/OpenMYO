# Contributing to OpenMyo

Thanks for helping. Please read [CODESTANDARDS.md](CODESTANDARDS.md) first; it is the source of
truth for review. AI agents should also read [CLAUDE.md](CLAUDE.md).

## Setup

1. Install the .NET 10 SDK (see `global.json`).
1. Clone the repository and run `dotnet build OpenMyo.sln`.
1. To run the app, register your own Yoto app as described in
   [Quickstart: Sign in and run OpenMyo](docs/user/quickstart.md) and paste the client id into
   **Settings**.

## Start here

| If you're… | Read |
|---|---|
| New to the code | [Architecture overview](docs/contribute/architecture/overview.md), then the [invariants](docs/contribute/architecture/invariants.md) |
| Adding a page or panel | [Navigation and commands](docs/contribute/concepts/navigation-and-commands.md), then [Add a page to the navigation rail](docs/contribute/how-to/add-navigation-page.md) or [Add a panel to a screen](docs/contribute/how-to/add-panel.md) |
| Adding a Yoto API call or card field | [Call a new Yoto endpoint](docs/contribute/how-to/call-yoto-endpoint.md), [Store a new card field](docs/contribute/how-to/add-card-field.md) |
| Adding a setting or secret | [Add a setting or a secret](docs/contribute/how-to/add-setting-or-secret.md) |
| Adding a Claude Desktop tool | [Add a Claude Desktop tool](docs/contribute/how-to/add-claude-desktop-tool.md) |
| Fixing icon matches that look wrong | [Improve icon matches](docs/contribute/how-to/improve-icon-matches.md), background in [Icon matching](docs/contribute/concepts/icon-matching.md) |
| Changing import, sign-in or saving | The matching page under [docs/contribute/concepts/](docs/contribute/concepts) |

Every page is listed in [docs/toc.yml](docs/toc.yml). New pages follow the Microsoft Learn conventions the
existing ones use: front matter with `title`, `description`, `ms.topic` and `ms.date`, one topic per page,
and a `## Related content` section at the end. Add each new page to `docs/toc.yml`.

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
- UI follows MVVM, compiled bindings, Discord-style design, keyboard and accessibility rules (section 8)
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
