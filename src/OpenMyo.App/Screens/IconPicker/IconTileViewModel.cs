using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Services;
using OpenMyo.Core.Icons;

namespace OpenMyo.App.Screens.IconPicker;

/// <summary>One selectable icon in the picker grid.</summary>
public sealed partial class IconTileViewModel : ObservableObject
{
    private readonly Action<IconTileViewModel> _choose;

    [ObservableProperty]
    private Bitmap? _image;

    public IconTileViewModel(DisplayIcon icon, Action<IconTileViewModel> choose)
    {
        Icon = icon;
        _choose = choose;
    }

    public DisplayIcon Icon { get; }

    public string Name => string.IsNullOrEmpty(Icon.Title) ? "Custom icon" : Icon.Title;

    public async Task LoadImageAsync(IconBitmapLoader loader) => Image = await loader.LoadIconAsync(Icon);

    [RelayCommand]
    private void Choose() => _choose(this);
}
