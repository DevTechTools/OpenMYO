---
title: Add a page to the navigation rail
description: Add a full-screen page, such as Import or Settings, to the OpenMyo navigation rail with a view model, view, registration, template and shell hook-up.
ms.topic: how-to
ms.date: 11/16/2026
---

# Add a page to the navigation rail

A page is a full screen chosen from the navigation rail, like Import or Settings. In this article, you add a page called
`Reports`; replace the name with yours.

## Steps

1. **Create the view model** in `src/OpenMyo.App/Screens/Reports/`. Derive from
   [`OperationViewModel`](../../../src/OpenMyo.App/Screens/OperationViewModel.cs) if the page runs long work, and take
   services through the constructor. [`SettingsViewModel`](../../../src/OpenMyo.App/Screens/Settings/SettingsViewModel.cs) is the pattern to copy.

1. **Create the view** beside it: a `UserControl` with `x:DataType` set, a `largeTitle` heading, and the error
   banner bound to `Error`. Copy the shape of [`SettingsView`](../../../src/OpenMyo.App/Screens/Settings/SettingsView.axaml).

1. **Register the view model** as a singleton in
   [`ServiceRegistration`](../../../src/OpenMyo.App/Composition/ServiceRegistration.cs). The container validates
   on build, so a missing registration fails at start-up.

1. **Map the view model to the view** with a `DataTemplate` in
   [`MainWindow.axaml`](../../../src/OpenMyo.App/Screens/Shell/MainWindow.axaml), next to the existing ones:

   ```xml
   <DataTemplate DataType="rep:ReportsViewModel"><rep:ReportsView /></DataTemplate>
   ```

1. **Add a rail icon**: a `StreamGeometry` in [`Styles/Theme.axaml`](../../../src/OpenMyo.App/Styles/Theme.axaml)
   next to the existing icons.

1. **Add the destination** to `SidebarDestination` in
   [`SidebarItem.cs`](../../../src/OpenMyo.App/Screens/Shell/SidebarItem.cs).

1. **Hook it into the shell** in [`MainViewModel`](../../../src/OpenMyo.App/Screens/Shell/MainViewModel.cs): take the
   view model in the constructor, expose it, add a sidebar item, and add a case to `Navigate`. If the shell's
   constructor is getting large, follow [CODESTANDARDS.md](../../../CODESTANDARDS.md) §2.

   If the new page must react to another page (for example refresh after an import), add that link to
   [`ScreenLinks`](../../../src/OpenMyo.App/Composition/ScreenLinks.cs), not to either page.

## Verify

Run `dotnet build OpenMyo.sln`, run the app, click the new rail icon, and check the page
with the keyboard alone.

## Related content

- [Navigation and commands](../concepts/navigation-and-commands.md)
- [Add a panel to an existing screen](add-panel.md)
