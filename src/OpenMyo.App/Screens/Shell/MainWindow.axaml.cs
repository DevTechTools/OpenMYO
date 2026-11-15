using Avalonia.Controls;

namespace OpenMyo.App.Screens.Shell;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
