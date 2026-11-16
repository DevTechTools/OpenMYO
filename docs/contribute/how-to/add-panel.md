---
title: Add a panel to an existing screen
description: Add a panel, such as the title clean-up panel or the icon picker, to an existing OpenMyo screen with a child view model and an embedded view.
ms.topic: how-to
ms.date: 11/16/2026
---

# Add a panel to an existing screen

A panel is part of a screen, like the title clean-up panel in the editor or the icon picker beside the
import list. In this article, you add a panel called `ChapterNotes` to the editor.

## Steps

1. **Create the view model** in the screen's folder, for example
   `Screens/Editor/ChapterNotesViewModel.cs`. Derive from `ObservableObject`, like
   [`TitleCleanupViewModel`](../../../src/OpenMyo.App/Screens/Editor/TitleCleanupViewModel.cs) and [`IconPickerViewModel`](../../../src/OpenMyo.App/Screens/IconPicker/IconPickerViewModel.cs).

1. **Expose it from the parent view model** as a read-only property:
   - if it only needs the parent's state, create it in the parent's constructor, as
     [`PlaylistEditorViewModel`](../../../src/OpenMyo.App/Screens/Editor/PlaylistEditorViewModel.cs) does with `Cleanup = new TitleCleanupViewModel(Chapters, Selection)`;
   - if it needs services, register it in [`ServiceRegistration`](../../../src/OpenMyo.App/Composition/ServiceRegistration.cs) (transient when each screen needs
     its own instance, as with `IconPickerViewModel`) and take it in the parent's constructor.

1. **Create the view.** Create `Screens/Editor/ChapterNotesView.axaml` as a `UserControl` with
   `x:DataType="vm:ChapterNotesViewModel"`.

1. **Embed it** in the parent view, binding its `DataContext` to the property, as
   [`PlaylistEditorView.axaml`](../../../src/OpenMyo.App/Screens/Editor/PlaylistEditorView.axaml) does:

   ```xml
   <editor:ChapterNotesView DataContext="{Binding Notes}" IsVisible="{Binding IsOpen}" />
   ```

## Verify

Build, run, and open the panel from the screen with the mouse and the keyboard.

## Related content

- [Navigation and commands](../concepts/navigation-and-commands.md)
- [Add a page to the navigation rail](add-navigation-page.md)
- [Library and editor](../concepts/library-and-editor.md)
