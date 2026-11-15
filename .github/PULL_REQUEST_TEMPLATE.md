## What and why

<!-- One or two sentences. Link the issue if there is one. -->

## Checklist

- [ ] `dotnet build` and `dotnet test` pass locally (warnings are errors)
- [ ] `bash eng/check-file-sizes.sh` passes (CODESTANDARDS section 2)
- [ ] Dependency rules respected; Core references no other project (section 1)
- [ ] Async I/O takes a `CancellationToken`; no blocking on tasks (section 5)
- [ ] No secrets in settings, logs or source (section 7)
- [ ] Tests added or updated; bug fixes include a failing-first test (section 9)
- [ ] UI changes follow section 8 (compiled bindings, Discord-style design, keyboard, accessibility)
- [ ] New packages justified below and added only to `Directory.Packages.props` (section 11)
- [ ] README / `docs/ARCHITECTURE.md` / CODESTANDARDS updated if behaviour or rules changed

## Notes for reviewers

<!-- Screenshots for UI changes, new dependencies, trade-offs. -->
