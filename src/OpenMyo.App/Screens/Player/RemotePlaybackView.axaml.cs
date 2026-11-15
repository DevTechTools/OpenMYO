using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// Seeking on a Yoto player restarts the track, so it must happen only when the user lets go of the slider (or
/// releases an arrow key on it), never when the slider moves by itself as the player reports a new length.
/// </summary>
public sealed partial class RemotePlaybackView : UserControl
{
    public RemotePlaybackView()
    {
        InitializeComponent();
        // The slider handles pointer events itself, so listen to handled ones too.
        SeekSlider.AddHandler(PointerPressedEvent, (_, _) => Model?.BeginSeek(), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        SeekSlider.AddHandler(PointerReleasedEvent, (_, _) => Seek(), RoutingStrategies.Bubble, handledEventsToo: true);
        SeekSlider.AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private RemotePlaybackViewModel? Model => DataContext as RemotePlaybackViewModel;

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown)
        {
            Seek();
        }
    }

    private void Seek() => Model?.SeekTo(SeekSlider.Value);
}
