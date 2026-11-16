# OpenMyo Code Standards

These standards are the source of truth for every change in this repository, whether written by a
person or an AI agent. Pull requests that break a **must** rule are not merged. **Should** rules may be
broken with a one-line comment explaining why.

The priorities, in order: **readability, correctness, efficiency, brevity.** Code is read far more
often than it is written; optimise for the next maintainer.

---

## 1. Solution layout

```
src/
  OpenMyo.Core/            Pure domain logic. No UI, no HTTP, no file-format or OS APIs.
  OpenMyo.Yoto/            Yoto Web API client (auth, cards, media, icons) and yotoicons.com search.
  OpenMyo.Claude/          Optional Claude icon suggestions (Anthropic SDK).
  OpenMyo.Mcp/             MCP server (stdio) that lets Claude Desktop manage track titles and icons.
  OpenMyo.Infrastructure/  Local machine: audio tags, secret storage, settings, paths.
  OpenMyo.App/             Avalonia desktop shell: views, view models, composition root.
tests/
  OpenMyo.<Project>.Tests/ One test project per production project that has logic.
```

### Dependency rules (must)

```
App ──► Core, Yoto, Claude, Mcp, Infrastructure
Yoto, Claude, Mcp, Infrastructure ──► Core
Core ──► (nothing but the BCL and logging abstractions)
```

- **Core never references another OpenMyo project** or any UI/HTTP/platform package.
- Outer projects implement interfaces declared in `OpenMyo.Core/Abstractions`. If an outer project
  needs a new capability from another outer project, add an interface to Core.
- Only `OpenMyo.App` knows about concrete implementations (composition root).
- Each library exposes one `ServiceCollectionExtensions.AddXxx()` method that registers its services.

### Folders (must)

- Organise **by feature**, not by type: `Icons/`, `Import/`, `Playlists/`, not `Services/`, `Helpers/`.
- Namespace equals folder path (`OpenMyo.Core.Icons` lives in `src/OpenMyo.Core/Icons`).
- No `Utils`, `Helpers`, `Common` or `Misc` folders or classes. Name things after what they do.

## 2. Size limits

| Item | Target | Hard limit (must) |
|---|---|---|
| File (`.cs`) | ≤ 300 lines | 500 lines |
| View (`.axaml`) | ≤ 300 lines | 500 lines; split into a `UserControl` |
| Method | ≤ 20 lines | 40 lines |
| Parameters | ≤ 4 | 6; introduce a parameter record |
| Nesting depth | ≤ 2 | 3; use guard clauses or extract a method |
| Constructor dependencies | ≤ 6 | 8; the class does too much, so split it |

`eng/check-file-sizes.sh` enforces the file limits in CI. Generated files are exempt.

**Exceptions.** A file may go over the hard limit when splitting it would make it harder to read (for
example one long list of styles). Opt it out with a marker in its first five lines that says why:

```csharp
// size-limit-exempt: one style per control; splitting scatters related selectors
```

```xml
<!-- size-limit-exempt: one style per control; splitting scatters related selectors -->
```

The check script skips files with the marker and a reason, and lists them so reviewers see each one.
The other limits in this table have no marker; break a **must** only with the reviewer's agreement and
a one-line comment explaining why.

## 3. Design principles

- **Single responsibility.** A class has one reason to change. If its name needs "And", split it.
- **One public type per file**, file named after the type. Small private/nested helpers and a type's
  own extension class may share the file.
- **Depend on abstractions** at layer boundaries (interfaces in Core). Inside a layer, prefer concrete
  sealed classes; do not create an interface that only has one implementation and no seam purpose.
- **Composition over inheritance.** Classes are `sealed` unless designed for inheritance.
- **Immutable data.** Domain models are `sealed record`s with `init` properties. Collections exposed as
  `IReadOnlyList<T>` / `IReadOnlyDictionary<K,V>`. Mutation happens through `with` expressions.
- **Pure functions where possible.** Text cleanup, ordering and matching are static/pure and unit-tested.
- **No static mutable state.** No service locators. Dependencies come through constructors.
- **YAGNI.** Do not add options, abstractions or extension points without a current caller.
- **Fail fast at boundaries:** validate public inputs with `ArgumentNullException.ThrowIfNull`,
  `ArgumentException.ThrowIfNullOrWhiteSpace`, etc. Trust internal callers.

## 4. C# style

- File-scoped namespaces; `using` directives outside the namespace; System usings first.
- Nullable reference types are on; **never** suppress with `!` unless a comment proves non-null.
- `var` when the type is obvious from the right-hand side; explicit types otherwise.
- Primary constructors for DI-only classes. Assign to `readonly` fields only when the parameter is
  captured by more than simple use.
- Expression-bodied members for single-line members only.
- `switch` expressions over `if/else` chains for mapping values.
- Guard clauses and early returns instead of nested `if`s.
- No magic numbers or strings: use a named `const` or `static readonly` near where it is used.
- String comparisons always specify `StringComparison`; formatting/parsing specifies
  `CultureInfo.InvariantCulture` unless the text is shown to the user.
- `TreatWarningsAsErrors` is on. Analyzer suppressions go in `.editorconfig` with a reason, never inline
  `#pragma` without a comment.

### Naming

| Kind | Style | Example |
|---|---|---|
| Types, methods, properties, constants | PascalCase | `IconIndex`, `MaxResults` |
| Interfaces | `I` + PascalCase | `IIconCatalog` |
| Private fields | `_camelCase` | `_httpClient` |
| Locals, parameters | camelCase | `trackTitle` |
| Async methods | `Async` suffix | `LoadAsync` |
| Booleans | Reads as a question | `IsSignedIn`, `HasIcon` |
| View models | `XxxViewModel` | `LibraryViewModel` |
| Views | `XxxView` / `XxxWindow` | `LibraryView` |

Names are whole words (`playlist`, not `pl`); loop indexes and lambdas over obvious items excepted.

## 5. Async, cancellation and errors

- Every I/O method is async, returns `Task`/`Task<T>`/`ValueTask`, and takes a `CancellationToken`
  as its **last** parameter. Pass the token all the way down.
- Never block on async code (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`).
- `async void` only for UI event handlers; prefer `[RelayCommand]` async commands.
- Work that is started but not awaited goes through `Forget(logger, description)` in the app, so its failure
  is logged. A bare `_ = DoAsync()` loses exceptions; use it only for a method documented never to throw.
- Bounded parallelism for batches: `Parallel.ForEachAsync` with `MaxDegreeOfParallelism`, or a
  `SemaphoreSlim`. Never fire unbounded `Task.WhenAll` over user-sized input.
- Exceptions are for exceptional situations. Expected outcomes (no match found, not signed in) are
  return values.
- Catch only exceptions you can handle; never `catch (Exception)` except at the UI boundary, where the
  error is logged and shown to the user.
- Throw specific exception types with messages that say what happened and what to do.
- `IDisposable`/`IAsyncDisposable` resources are disposed with `using`.

## 6. Efficiency

- Stream files; never load a whole audio file into memory. Hash and upload from a `FileStream`.
- Cache anything slow and stable: icon catalog (disk, days), card details (keyed by `updatedAt`),
  icon bitmaps (memory + disk).
- Use `HttpClient` through `IHttpClientFactory`/typed clients only; never `new HttpClient()` per call.
- Precompute indexes (e.g. trigram inverted index) once; query many times.
- Avoid LINQ in hot loops when it allocates per item; prefer clear loops there. Elsewhere LINQ is fine.
- Load lists progressively: show summaries immediately, fill in details as they arrive.

## 7. Security and privacy

- **Secrets never touch plain-text files, logs, URLs or settings JSON.** Refresh tokens and API keys
  live in `ISecretStore` (macOS Keychain, Windows DPAPI).
- Never log tokens, API keys, or full request bodies containing them.
- OAuth uses Authorization Code + PKCE on a loopback redirect (no client secret in the binary).
- Treat file names, tags, and API responses as untrusted input: validate lengths and formats before
  sending them back to Yoto.

## 8. UI (Avalonia)

- **MVVM, strictly.** Views contain layout only. Code-behind is limited to view-only concerns
  (focus, drag-and-drop plumbing, picker invocation) and must stay under 60 lines.
- **Feature folders.** Each feature is one folder, `Screens/<Feature>/`, holding its views (`XxxView.axaml` and
  `.axaml.cs`) beside their view models (`XxxViewModel.cs`), plus row/tile view models and helpers. A view model
  is always named after its view. When a folder passes about 12 files, move one cohesive group into a
  subfolder named for what it holds, such as `Editor/Commands/`. Namespaces follow folders.
- Use `CommunityToolkit.Mvvm` source generators: `[ObservableProperty]`, `[RelayCommand]`.
- **Compiled bindings everywhere** (`x:DataType` on every view and `DataTemplate`).
- View models do not reference Avalonia controls. Platform services (file pickers, clipboard) are
  injected behind small interfaces.
- **Design language: Discord / GitKraken-inspired desktop UI. Never Material.** Dark-first layered
  surfaces (rail → sidebar → content → cards get progressively lighter), one blurple accent (`#5865F2`),
  filled inputs with an accent focus ring, soft hover/selected fills instead of outlines, pill badges for
  counts (red = needs attention), 4 px control and 8 px card radii, compact rows, sidebar + content layout.
  No floating action buttons, ripples, heavy drop shadows or all-caps buttons.
- Dark theme by default with a matching light palette. All colours come from tokens in
  `Styles/Theme.axaml`, never inline hex.
- **Fewer clicks.** Primary actions are one click from where the user is looking. Bulk actions act on
  the current selection. Every destructive action is undoable or confirmed, not both.
- Keyboard first: every command reachable by keyboard; standard shortcuts (`Cmd/Ctrl+S`, `Delete`,
  `Cmd/Ctrl+F`, `Cmd/Ctrl+O`).
- Accessible: every icon-only button has `ToolTip.Tip` and `AutomationProperties.Name`.
- Long operations show inline progress and can be cancelled; the UI never freezes.

## 9. Testing

- Core logic (text cleanup, ordering, matching, health checks, JSON mapping) has unit tests.
- Test names: `Method_Scenario_ExpectedResult`.
- Arrange / Act / Assert, one behaviour per test. Use `[Theory]` for input tables.
- Tests never hit the network or real keychains. Use fakes for Core interfaces and
  `HttpMessageHandler` stubs for HTTP.
- A bug fix comes with a test that fails without the fix.

## 10. Comments and documentation

- Public types and non-obvious public members get `///` XML docs that explain **why** or
  **what for**, not a restatement of the name.
- Comments explain intent, constraints and external facts (e.g. "Yoto refresh tokens are single-use").
  Do not comment what the code already says.
- No commented-out code. No `TODO` without a linked issue.

## 11. Dependencies

- Package versions live only in `Directory.Packages.props` (central package management).
- Add a package only when it removes meaningful code; prefer the BCL. Justify new packages in the PR.
- Pin exact versions. Update deliberately, with a changelog check.

## 12. Git and pull requests

- Small, focused commits in imperative mood: `Add trigram icon index`.
- One logical change per pull request; CI (build + tests + size check on macOS and Windows) must pass.
- Update `README.md`, the pages under `docs/` (user pages for behaviour, `docs/contribute/` for design and
  workflow, plus `docs/toc.yml` for new pages) or this file in the same PR
  when behaviour or rules change.
