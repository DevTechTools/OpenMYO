using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace OpenMyo.App.Screens.Import.Feed;

/// <summary>Code-behind only puts the cursor in the address box when the dialog opens (view-only focus).</summary>
public sealed partial class FeedImportView : UserControl
{
    public FeedImportView() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsVisible)
        {
            Dispatcher.UIThread.Post(() => UrlBox.Focus());
        }
    }
}
