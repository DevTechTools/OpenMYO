using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace OpenMyo.App.Controls;

/// <summary>
/// An image scaled with nearest-neighbour sampling, so 16x16 Yoto icons stay crisp when enlarged,
/// exactly like the player's pixel display.
/// </summary>
public sealed class PixelImage : Image
{
    public PixelImage() => RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

    // Reuse Image's control theme; this type only changes how bitmaps are sampled.
    protected override Type StyleKeyOverride => typeof(Image);
}
