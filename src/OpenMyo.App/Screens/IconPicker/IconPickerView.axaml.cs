using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace OpenMyo.App.Screens.IconPicker;

public sealed partial class IconPickerView : UserControl
{
    public IconPickerView()
    {
        InitializeComponent();

        // Choosing a tile moves on to the next track; keep typing in the search box instead of on the tile.
        AddHandler(Button.ClickEvent, OnTileClicked, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnTileClicked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && button.Classes.Contains("iconTile"))
        {
            // After the command has moved on and the query has changed, so the new text is what gets selected.
            Dispatcher.UIThread.Post(() =>
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
            });
        }
    }
}
