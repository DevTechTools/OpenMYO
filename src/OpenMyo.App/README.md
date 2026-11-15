---
description: Guide to the OpenMyo.App project; read when adding or changing a screen, panel, view model or style.
---

# OpenMyo.App

The Avalonia desktop UI and the composition root. Screens follow MVVM: a view model holds state and
commands, a view (`.axaml`) binds to it with compiled bindings, and nothing in a view calls services.

## File map

| Path | Owns |
|---|---|
| [`Program.cs`](Program.cs) | Entry point; chooses window mode or connector mode (`--mcp`) |
| `App.axaml(.cs)` | Loads styles, builds services, opens [`MainWindow`](Screens/Shell/MainWindow.axaml) |
| [`Composition/ServiceRegistration.cs`](Composition/ServiceRegistration.cs) | Every service and view model registration |
| [`Composition/ScreenLinks.cs`](Composition/ScreenLinks.cs) | Every link between pages (sign-in, import → library, library → icon index) |
| [`Composition/McpMode.cs`](Composition/McpMode.cs) | Connector mode: no window |
| [`Screens/Shell/MainViewModel.cs`](Screens/Shell/MainViewModel.cs) | The shell: navigation rail items, navigation, sign-in state |
| [`Screens/Shell/SidebarItem.cs`](Screens/Shell/SidebarItem.cs) | Rail entries and the `SidebarDestination` enum |
| [`Screens/OperationViewModel.cs`](Screens/OperationViewModel.cs) | Base class for screens that run long work |
| `Screens/<Feature>/` | One folder per feature: its views (`.axaml` + `.axaml.cs`) beside their view models, plus row/tile view models and helpers |
| [`Screens/Editor/Commands/`](Screens/Editor/Commands) | The editor's command groups and the interface they use to reach it |
| [`Screens/Shell/MainWindow.axaml`](Screens/Shell/MainWindow.axaml) | Rail layout and the `DataTemplate` that maps each page view model to its view |
| [`Styles/Theme.axaml`](Styles/Theme.axaml) | Colours, brushes, corner radii and rail icon glyphs (`StreamGeometry`) |
| [`Styles/Controls.axaml`](Styles/Controls.axaml) | Control styles and classes such as `card`, `accent`, `largeTitle` |
| [`Services/`](Services) | UI-side services: file pickers, image cache, window access |
| [`Controls/`](Controls), [`Converters/`](Converters) | Reusable controls and binding converters |

## How a page is wired

```text
SidebarItem (MainViewModel.TopItems / BottomItems)
  → MainViewModel.SelectedItem
  → MainViewModel.Navigate sets CurrentPage to the page view model
  → MainWindow's ContentControl shows CurrentPage
  → MainWindow.DataTemplates picks the view for that view model type
  → the view binds to the view model (x:DataType)
```

A panel inside a page is a child view model exposed as a property of the page view model (for example
[`PlaylistEditorViewModel.Cleanup`](Screens/Editor/PlaylistEditorViewModel.cs)), shown by embedding its view in the page view with
`DataContext="{Binding Cleanup}"`.

## Which view shows which view model

```text
Feature      View                    View model                  Bound via
Shell        MainWindow              MainViewModel
Library      LibraryView             LibraryViewModel            CurrentPage
             BulkActionsView         BulkActionsViewModel        .Bulk
Editor       PlaylistEditorView      PlaylistEditorViewModel     .Editor
             TitleCleanupView        TitleCleanupViewModel       .Cleanup
Import       ImportView              ImportViewModel             CurrentPage
             DraftCardView           DraftCardViewModel          each draft
IconPicker   IconPickerView          IconPickerViewModel         .Picker (one per owner)
             CommunityResultsView    CommunityResultsViewModel   .Community
Player       PlayerBarView           PlayerBarViewModel          MainViewModel.PlayerBar (docked under every page)
             NowPlayingView          NowPlayingViewModel         .Local (plays on this computer)
             RemotePlaybackView      RemotePlaybackViewModel     .Remote (plays on a Yoto player)
             OutputPickerView        OutputPickerViewModel       .Output (where playback goes)
Settings     SettingsView            SettingsViewModel           CurrentPage
             ClaudeDesktopView       ClaudeDesktopViewModel      .ClaudeDesktop
             AiKeyView               AiKeyViewModel              .AiKey (one copy, also in the picker)
```

Rows and tiles in a list (`PlaylistRowViewModel`, `ChapterRowViewModel`, `DraftTrackViewModel`, the icon
tiles, `SidebarItem`) are small `DataTemplate`s inside the list's view, so their view models sit in that
feature folder. A row that grows past a few lines of markup gets its own view.

## How view models talk to each other

- **Parent → child:** a parent owns its children (constructor or property) and calls them directly.
- **Child → parent:** a child raises an event about itself (`IconChosen`, `RowsDeleted`); it never holds its
  parent. Where a child needs the parent's services, it gets a small interface (`IChapterEditor`,
  `IReorderableOwner`), not the parent type.
- **Page → page:** never directly. [`Composition/ScreenLinks.cs`](Composition/ScreenLinks.cs) subscribes to
  page events and calls the other page, so every cross-page link is in one file.

## Local rules

The full UI rules are CODESTANDARDS §8. The ones that most often catch new screens:

- View models do not reference Avalonia controls. Code-behind is for view-only concerns and stays under
  60 lines.
- Every view and `DataTemplate` sets `x:DataType`.
- Long work goes through [`OperationViewModel.RunAsync`](Screens/OperationViewModel.cs), with inline progress and a Cancel button.
- Colours come from tokens in [`Styles/Theme.axaml`](Styles/Theme.axaml), never inline hex.
- Every command is reachable by keyboard; icon-only buttons have `ToolTip.Tip` and
  `AutomationProperties.Name`.
- A view model is named after its view and sits beside it: `FooView.axaml` ↔ `FooViewModel.cs`.
- When a feature folder passes about 12 files, move one group into a subfolder named for what it holds
  (as `Editor/Commands/` does).
- Never start work with `_ = SomethingAsync()`; use `SomethingAsync().Forget(logger, "what it is")`
  ([`BackgroundWork`](Services/BackgroundWork.cs)) so a failure is logged instead of lost. The one exception is
  [`IconBitmapLoader`](Services/IconBitmapLoader.cs), which never throws.
- Logic worth testing goes in `OpenMyo.Core`; this project has no test project.
- [`MainViewModel`](Screens/Shell/MainViewModel.cs) has 5 constructor dependencies and the hard limit is 8 (CODESTANDARDS §2).

## Common changes

- [How to add a page to the navigation rail](../../docs/how-to.md#how-to-add-a-page-to-the-navigation-rail)
- [How to add a panel to an existing screen](../../docs/how-to.md#how-to-add-a-panel-to-an-existing-screen)
- [How to add a setting or a secret](../../docs/how-to.md#how-to-add-a-setting-or-a-secret)
- Background: [The desktop app and the Claude Desktop connector](../../docs/design/app.md)
