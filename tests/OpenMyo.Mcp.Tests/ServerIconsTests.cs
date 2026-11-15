using OpenMyo.Mcp.Support;

namespace OpenMyo.Mcp.Tests;

public sealed class ServerIconsTests
{
    [Fact]
    public void Load_EmbeddedLogo_ReturnsPngThenSvgAsDataUris()
    {
        var icons = ServerIcons.Load();

        Assert.Equal(["image/png", "image/svg+xml"], icons.Select(icon => icon.MimeType));
        Assert.All(icons, icon => Assert.StartsWith($"data:{icon.MimeType};base64,", icon.Source, StringComparison.Ordinal));
    }
}
