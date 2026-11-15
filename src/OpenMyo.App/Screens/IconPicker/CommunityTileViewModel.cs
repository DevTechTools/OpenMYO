using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Services;
using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Icons;

namespace OpenMyo.App.Screens.IconPicker;

/// <summary>A community icon in the picker, credited to its artist. Choosing it imports it into the user's account.</summary>
public sealed partial class CommunityTileViewModel(CommunityIcon icon, Func<CommunityTileViewModel, Task> choose) : ObservableObject
{
    [ObservableProperty]
    private Bitmap? _image;

    [ObservableProperty]
    private bool _isImporting;

    public CommunityIcon Icon { get; } = icon;

    public string ToolTip => $"{Icon.Title}\n{Icon.Credit}";

    public async Task LoadImageAsync(IconBitmapLoader loader) =>
        Image = await loader.LoadIconAsync(new DisplayIcon(Icon.Id, Icon.Title, Icon.Tags, Icon.ImageUrl, IsPublic: false));

    [RelayCommand]
    private Task ChooseAsync() => choose(this);
}
