using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Tests.Icons;

public sealed class NumberIconsTests
{
    [Fact]
    public void Number_PublicNumberIcons_FoundByValue()
    {
        var index = IconIndex.Build([Icon("one", "Number - 1"), Icon("twelve", "Numbers - 12"), Icon("boy", "Boy 3")]);

        Assert.Equal("one", index.NumberIcon(1)?.MediaId);
        Assert.Equal("twelve", index.NumberIcon(12)?.MediaId);
        Assert.Null(index.NumberIcon(3));
    }

    [Fact]
    public void Number_PublicAndUserIconForSameNumber_PrefersPublic()
    {
        var index = IconIndex.Build([Icon("mine", "OpenMyo number 4", isPublic: false), Icon("yoto", "Numbers - 4")]);

        Assert.Equal("yoto", index.NumberIcon(4)?.MediaId);
    }

    [Fact]
    public void Number_OnlyUserIcon_IsFound()
    {
        var index = IconIndex.Build([Icon("mine", NumberIcons.DrawnTitle(31), isPublic: false)]);

        Assert.Equal("mine", index.NumberIcon(31)?.MediaId);
    }

    [Fact]
    public void Number_UserIconInOldDrawnStyle_IsIgnored()
    {
        var index = IconIndex.Build([Icon("old", "Numbers - 31", isPublic: false)]);

        Assert.Null(index.NumberIcon(31));
    }

    private static DisplayIcon Icon(string id, string title, bool isPublic = true) => new(id, title, [], null, isPublic);
}
