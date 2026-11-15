---
description: "Step-by-step guides for common changes: Yoto calls, card fields, domain logic, settings, Claude Desktop tools, pages and panels; read when adding something new."
---

# How-to guides

Every change also follows [CODESTANDARDS.md](../CODESTANDARDS.md) and passes the checks in
[CONTRIBUTING.md](../CONTRIBUTING.md#running-checks).

## How to call a new Yoto endpoint

1. Add request and response DTOs in [`Yoto/Json`](../src/OpenMyo.Yoto/Json) and register each type with a
   `[JsonSerializable]` line on [`YotoJsonContext`](../src/OpenMyo.Yoto/Json/YotoJsonContext.cs).
   Serialisation is source-generated, so an unregistered type fails at runtime.
2. Make the call in the class that owns the area: cards in [`YotoPlaylistRepository`](../src/OpenMyo.Yoto/Content/YotoPlaylistRepository.cs), media in
   [`YotoMediaUploader`](../src/OpenMyo.Yoto/Media/YotoMediaUploader.cs), icons in [`YotoIconCatalog`](../src/OpenMyo.Yoto/Icons/YotoIconCatalog.cs). Create the client with
   `httpClientFactory.CreateClient(YotoHttpClients.Api)` to get retries and auth, and read the response
   with [`YotoResponse.ReadAsync`](../src/OpenMyo.Yoto/Http/YotoResponse.cs) or `EnsureSuccessAsync`.
3. Check any id from outside with [`CardIds`](../src/OpenMyo.Yoto/Content/CardIds.cs) before it goes into a URL, and escape query values.
4. If Core needs the feature, add a method to the Core interface (or a new interface in
   [`Core/Abstractions`](../src/OpenMyo.Core/Abstractions)) and register the implementation in
   [`AddYoto`](../src/OpenMyo.Yoto/ServiceCollectionExtensions.cs).
5. Test it with [`StubHttpHandler`](../tests/OpenMyo.Yoto.Tests/Support/StubHttpHandler.cs) in
   [`tests/OpenMyo.Yoto.Tests`](../tests/OpenMyo.Yoto.Tests).
6. If it needs a new OAuth scope, add it to [`YotoAuthEndpoints.Scope`](../src/OpenMyo.Yoto/Auth/YotoAuthEndpoints.cs). Existing users are asked to sign in
   again on next launch ([Sign-in and tokens](design/yoto-client.md#sign-in-and-tokens)).

## How to store a new field on a card or chapter

1. Add the property to the record in [`Core/Playlists`](../src/OpenMyo.Core/Playlists).
2. Read it in [`CardMapper`](../src/OpenMyo.Yoto/Content/CardMapper.cs).
3. Write it in [`CardComposer`](../src/OpenMyo.Yoto/Content/CardComposer.cs) or
   [`ChapterComposer`](../src/OpenMyo.Yoto/Content/ChapterComposer.cs) by setting that one field on the
   cloned node. Never build the node from scratch
   ([Reading and saving cards](design/yoto-client.md#reading-and-saving-cards)).
4. Add a round-trip case to [`CardComposerTests`](../tests/OpenMyo.Yoto.Tests/Content/CardComposerTests.cs)
   with the sample JSON in [`Samples`](../tests/OpenMyo.Yoto.Tests/Content/Samples.cs), and assert that an
   unrelated field is still present after the save.

## How to add domain logic

A new title clean-up rule, library filter, matcher or similar.

1. Put the logic in the feature folder under [`OpenMyo.Core`](../src/OpenMyo.Core) as a pure class or
   static method. Take dependencies through Core interfaces only.
2. Register it in [`AddCore`](../src/OpenMyo.Core/ServiceCollectionExtensions.cs) if it has state or
   dependencies.
3. Test it in the matching folder of [`tests/OpenMyo.Core.Tests`](../tests/OpenMyo.Core.Tests), using the
   fakes in [`Fakes`](../tests/OpenMyo.Core.Tests/Fakes).
4. Call it from a view model ([How to add a page to the navigation rail](#how-to-add-a-page-to-the-navigation-rail), [How to add a panel to an existing screen](#how-to-add-a-panel-to-an-existing-screen)) or an MCP tool
   ([How to add a Claude Desktop tool](#how-to-add-a-claude-desktop-tool)).

Examples:

- **Title clean-up rule:** a property on [`TitleCleanupOptions`](../src/OpenMyo.Core/Text/TitleCleanupOptions.cs) and a step in [`TitleCleaner.Clean`](../src/OpenMyo.Core/Text/TitleCleaner.cs).
- **Library filter:** a flag in [`PlaylistIssues`](../src/OpenMyo.Core/Library/PlaylistIssues.cs), a check in [`PlaylistHealth`](../src/OpenMyo.Core/Library/PlaylistHealth.cs), and a chip in
  `LibraryViewModel.Chips` ([`LibraryViewModel`](../src/OpenMyo.App/Screens/Library/LibraryViewModel.cs)); matching and counts come from the flag.

## How to add a setting or a secret

**Setting** (anything not secret):

1. Add an `init` property with a default to [`AppSettings`](../src/OpenMyo.Core/Abstractions/ISettingsStore.cs).
   Existing `settings.json` files get the default.
2. Read it with [`ISettingsStore.LoadAsync`](../src/OpenMyo.Core/Abstractions/ISettingsStore.cs); save with `SaveAsync(current with { … })`.
3. Show it in [`SettingsViewModel`](../src/OpenMyo.App/Screens/Settings/SettingsViewModel.cs) and
   [`SettingsView`](../src/OpenMyo.App/Screens/Settings/SettingsView.axaml).

**Secret** (tokens, keys):

1. Add a name to [`SecretNames`](../src/OpenMyo.Core/Abstractions/SecretNames.cs).
2. Read and write it through [`ISecretStore`](../src/OpenMyo.Core/Abstractions/ISecretStore.cs). Never put it in `AppSettings`, a log or a URL.

## How to add a Claude Desktop tool

1. Add a public method with `[McpServerTool(Name = "verb_noun", …)]` and a `[Description]` on the method
   and each parameter, in a `[McpServerToolType]` class under [`Mcp/Playlists`](../src/OpenMyo.Mcp/Playlists)
   or [`Mcp/Icons`](../src/OpenMyo.Mcp/Icons). Set `ReadOnly`, `Destructive` and `Idempotent` to match
   what it does.
2. Wrap the body in [`ToolGuard.RunAsync`](../src/OpenMyo.Mcp/Support/ToolGuard.cs). Throw `McpException` with a plain sentence for bad input.
3. Load playlists through [`PlaylistLookup`](../src/OpenMyo.Mcp/Support/PlaylistLookup.cs), validate all
   input before saving anything ([`TrackRange`](../src/OpenMyo.Mcp/Support/TrackRange.cs)), save once, and
   return the previous values so Claude can undo.
4. Register a new tool class in [`AddMcpTools`](../src/OpenMyo.Mcp/ServiceCollectionExtensions.cs).
   [`ToolRegistry`](../src/OpenMyo.Mcp/Support/ToolRegistry.cs) finds its methods by attribute.
5. Describe when to use it in the `Instructions` text in [`McpEntry`](../src/OpenMyo.Mcp/McpEntry.cs).
6. Test it with [`McpWorld`](../tests/OpenMyo.Mcp.Tests/McpWorld.cs) in
   [`tests/OpenMyo.Mcp.Tests`](../tests/OpenMyo.Mcp.Tests).

## How to add a page to the navigation rail

A page is a full screen chosen from the navigation rail, like Import or Settings. The steps use a page called
`Reports`; replace the name with yours.

1. **View model.** Create `src/OpenMyo.App/Screens/Reports/ReportsViewModel.cs`; the page's view goes beside
   it. Derive from
   [`OperationViewModel`](../src/OpenMyo.App/Screens/OperationViewModel.cs) if the page runs long work, and take services through the constructor.

   ```csharp
   using CommunityToolkit.Mvvm.Input;
   using OpenMyo.Core.Abstractions;

   namespace OpenMyo.App.Screens.Reports;

   public sealed partial class ReportsViewModel(IPlaylistRepository repository) : OperationViewModel
   {
       [RelayCommand]
       private Task RefreshAsync() => RunAsync("Loading reports", async token => { /* … */ });
   }
   ```

2. **View.** Create `src/OpenMyo.App/Screens/Reports/ReportsView.axaml` and its code-behind. Copy the shape of
   [`SettingsView`](../src/OpenMyo.App/Screens/Settings/SettingsView.axaml): a `UserControl` with
   `x:DataType` set, a `largeTitle` heading, and the error banner bound to `Error`.

   ```xml
   <UserControl xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:vm="using:OpenMyo.App.Screens.Reports"
                x:Class="OpenMyo.App.Screens.Reports.ReportsView"
                x:DataType="vm:ReportsViewModel">
     <StackPanel Margin="32,40,32,24" Spacing="16">
       <TextBlock Classes="largeTitle" Text="Reports" />
     </StackPanel>
   </UserControl>
   ```

   ```csharp
   using Avalonia.Controls;

   namespace OpenMyo.App.Screens.Reports;

   public sealed partial class ReportsView : UserControl
   {
       public ReportsView() => InitializeComponent();
   }
   ```

3. **Register it** as a singleton in
   [`ServiceRegistration`](../src/OpenMyo.App/Composition/ServiceRegistration.cs):
   `services.AddSingleton<ReportsViewModel>();`. The container validates on build, so a missing
   registration fails at start-up.

4. **Map the view model to the view** in [`MainWindow.axaml`](../src/OpenMyo.App/Screens/Shell/MainWindow.axaml):
   add one `xmlns` line next to the existing ones, then a `DataTemplate` in `Window.DataTemplates`.

   ```xml
   xmlns:rep="using:OpenMyo.App.Screens.Reports"
   …
   <DataTemplate DataType="rep:ReportsViewModel"><rep:ReportsView /></DataTemplate>
   ```

5. **Add a rail icon**: a `StreamGeometry` (24×24 path) in
   [`Styles/Theme.axaml`](../src/OpenMyo.App/Styles/Theme.axaml) next to `IconSettings`, for example
   `x:Key="IconReports"`.

6. **Add the destination** `Reports` to the `SidebarDestination` enum in
   [`SidebarItem.cs`](../src/OpenMyo.App/Screens/Shell/SidebarItem.cs).

7. **Hook it into the shell** in [`MainViewModel`](../src/OpenMyo.App/Screens/Shell/MainViewModel.cs):
   - take `ReportsViewModel` in the constructor and expose it as a property;
   - add `new("Reports", "IconReports", SidebarDestination.Reports)` to `TopItems` (or `BottomItems`);
   - add `SidebarDestination.Reports => ReportsPage` to the switch in `Navigate`.

   `MainViewModel` has 4 constructor dependencies and the hard limit is 8. If the new page must react to
   another page (for example refresh after an import), add that link to
   [`ScreenLinks`](../src/OpenMyo.App/Composition/ScreenLinks.cs), not to either page. If more pages are
   added later, group the page view models into one record (for example `ShellPages`) instead of adding
   parameters.

8. **Check it**: `dotnet build OpenMyo.sln`, run the app, click the new rail icon, and check the page
   with the keyboard alone.

## How to add a panel to an existing screen

A panel is part of a screen, like the title clean-up panel in the editor or the icon picker beside the
import list.

1. **View model.** Create it in the screen's folder, for example
   `Screens/Editor/ChapterNotesViewModel.cs`. Derive from `ObservableObject`, like
   [`TitleCleanupViewModel`](../src/OpenMyo.App/Screens/Editor/TitleCleanupViewModel.cs) and [`IconPickerViewModel`](../src/OpenMyo.App/Screens/IconPicker/IconPickerViewModel.cs).
2. **Expose it from the parent view model** as a read-only property:
   - if it only needs the parent's state, create it in the parent's constructor, as
     [`PlaylistEditorViewModel`](../src/OpenMyo.App/Screens/Editor/PlaylistEditorViewModel.cs) does with `Cleanup = new TitleCleanupViewModel(Chapters, Selection)`;
   - if it needs services, register it in [`ServiceRegistration`](../src/OpenMyo.App/Composition/ServiceRegistration.cs) (`AddTransient` when each screen needs
     its own instance, as with `IconPickerViewModel`) and take it in the parent's constructor.
3. **View.** Create `Screens/Editor/ChapterNotesView.axaml` as a `UserControl` with
   `x:DataType="vm:ChapterNotesViewModel"`.
4. **Embed it** in the parent view, binding its `DataContext` to the property, as
   [`PlaylistEditorView.axaml`](../src/OpenMyo.App/Screens/Editor/PlaylistEditorView.axaml) does:

   ```xml
   <editor:ChapterNotesView DataContext="{Binding Notes}" IsVisible="{Binding IsOpen}" />
   ```

5. **Check it**: build, run, and open the panel from the screen with the mouse and the keyboard.

## How to render a screen without signing in

[`tools/OpenMyo.Snapshots`](../tools/OpenMyo.Snapshots) renders the playlist editor headlessly with the real
styles and saves PNGs. Add a block there that opens your screen's view model, then run:

```bash
dotnet run --project tools/OpenMyo.Snapshots -- <output folder> [card json]
```

## How to improve icon matches

1. Add the failing title as a test case in
   [`IconAutoAssignerTests`](../tests/OpenMyo.Core.Tests/Icons/IconAutoAssignerTests.cs) or
   [`IconQueryTests`](../tests/OpenMyo.Core.Tests/Text/IconQueryTests.cs).
2. Make the smallest change that fixes it, in this order of preference:
   1. A related word is missing: add a line to [`Synonyms.txt`](../src/OpenMyo.Core/Text/Synonyms.txt).
   2. A filler word is winning: add it to [`WeakWords.txt`](../src/OpenMyo.Core/Text/WeakWords.txt),
      sorted, lower case, singular.
   3. Title noise reaches the query: extend [`IconQuery`](../src/OpenMyo.Core/Text/IconQuery.cs).
   4. Otherwise, change weights or thresholds in [`IconScorer`](../src/OpenMyo.Core/Icons/IconScorer.cs), [`IconSuggester`](../src/OpenMyo.Core/Icons/IconSuggester.cs) or [`CommunityIconFallback`](../src/OpenMyo.Core/Icons/CommunityIconFallback.cs).
