using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace OpenMyo.App.Controls;

/// <summary>
/// Lets the user drag rows of an <see cref="ItemsControl"/> by any element with the <c>dragHandle</c> class.
/// Rows move live under the pointer; the items control's DataContext must be an <see cref="IReorderableList"/>.
/// </summary>
public static class DragReorder
{
    private const string HandleClass = "dragHandle";
    private const string DraggingClass = "dragging";

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, bool>("IsEnabled", typeof(DragReorder));

    private static readonly AttachedProperty<object?> DraggedItemProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, object?>("DraggedItem", typeof(DragReorder));

    static DragReorder() => IsEnabledProperty.Changed.AddClassHandler<ItemsControl>(OnIsEnabledChanged);

    public static bool GetIsEnabled(ItemsControl control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ItemsControl control, bool value) => control.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(ItemsControl list, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not true)
        {
            return;
        }

        list.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerReleasedEvent, (sender, _) => EndDrag(sender), RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerCaptureLostEvent, (sender, _) => EndDrag(sender), RoutingStrategies.Tunnel);
    }

    private static void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ItemsControl list || !e.GetCurrentPoint(list).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var handle = (e.Source as Visual)?.GetSelfAndVisualAncestors()
            .OfType<StyledElement>().FirstOrDefault(element => element.Classes.Contains(HandleClass));
        if (handle?.DataContext is not { } item)
        {
            return;
        }

        list.SetValue(DraggedItemProperty, item);
        SetDragging(list, item, true);
        e.Pointer.Capture(list);
        e.Handled = true;
    }

    private static void OnMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not ItemsControl { DataContext: IReorderableList target } list
            || list.GetValue(DraggedItemProperty) is not { } item)
        {
            return;
        }

        var from = list.IndexFromContainer(list.ContainerFromItem(item) ?? list);
        var to = IndexUnderPointer(list, e);
        if (from < 0 || to < 0 || from == to)
        {
            return;
        }

        target.MoveItem(from, to);
        SetDragging(list, item, true);
    }

    private static int IndexUnderPointer(ItemsControl list, PointerEventArgs e)
    {
        for (var index = 0; index < list.ItemCount; index++)
        {
            if (list.ContainerFromIndex(index) is { } container
                && e.GetPosition(container) is var point
                && point.Y >= 0 && point.Y < container.Bounds.Height)
            {
                return index;
            }
        }

        return -1;
    }

    private static void EndDrag(object? sender)
    {
        if (sender is ItemsControl list && list.GetValue(DraggedItemProperty) is { } item)
        {
            SetDragging(list, item, false);
            list.SetValue(DraggedItemProperty, null);
        }
    }

    private static void SetDragging(ItemsControl list, object item, bool isDragging) =>
        list.ContainerFromItem(item)?.Classes.Set(DraggingClass, isDragging);
}
