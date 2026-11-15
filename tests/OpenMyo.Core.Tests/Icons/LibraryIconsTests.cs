using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Core.Tests.Icons;

public sealed class LibraryIconsTests
{
    [Fact]
    public void FromPlaylists_IconUsedTwice_ReturnsOneIconTaggedWithBothTitles()
    {
        var playlist = Playlist(Chapter("Connie, Part 1", "abc"), Chapter("Whit's End", "abc"), Chapter("No icon", null));

        var icons = LibraryIcons.FromPlaylists([playlist]);

        var icon = Assert.Single(icons);
        Assert.Equal("abc", icon.MediaId);
        Assert.Contains("connie", icon.Tags);
        Assert.False(icon.IsPublic);
    }

    [Fact]
    public void FromPlaylists_LibraryIcon_IsFoundBySearch()
    {
        var icons = LibraryIcons.FromPlaylists([Playlist(Chapter("Dragon Adventure", "dragon-id"))]);

        var index = IconIndex.Build(icons);

        Assert.Equal("dragon-id", index.Search("dragon", 5)[0].Icon.MediaId);
    }

    private static Playlist Playlist(params Chapter[] chapters) =>
        new() { CardId = "card", Title = "Playlist", Chapters = chapters };

    private static Chapter Chapter(string title, string? iconId) =>
        new() { Key = title, Title = title, IconMediaId = iconId };
}
