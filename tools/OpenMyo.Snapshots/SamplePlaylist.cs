using System.Globalization;
using System.Text.Json.Nodes;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Snapshots;

/// <summary>Builds the playlist to render: from a cached card JSON when given, otherwise a small sample.</summary>
internal static class SamplePlaylist
{
    private static readonly string[] SampleTitles = ["01 - the_moon.mp3", "02 - Owl Babies (Official Audio)", "03 - GOODNIGHT GORILLA"];

    public static Playlist Load(string? cardJsonPath)
    {
        if (cardJsonPath is null || !File.Exists(cardJsonPath))
        {
            return Sample();
        }

        var root = JsonNode.Parse(File.ReadAllText(cardJsonPath));
        var card = root?["card"] ?? root;
        var chapters = (card?["content"]?["chapters"]?.AsArray() ?? []).Select(ToChapter).OfType<Chapter>().ToList();
        var cover = card?["metadata"]?["cover"]?["imageL"]?.GetValue<string>();
        return new Playlist
        {
            CardId = card?["cardId"]?.GetValue<string>(),
            Title = card?["title"]?.GetValue<string>() ?? "Sample",
            CoverImageUrl = cover is null ? null : new Uri(cover),
            Chapters = chapters,
        };
    }

    private static Chapter? ToChapter(JsonNode? node, int index)
    {
        if (node is null)
        {
            return null;
        }

        var title = node["title"]?.GetValue<string>() ?? $"Track {index + 1}";
        var icon = DisplayIcon.MediaIdFromUri(node["display"]?["icon16x16"]?.GetValue<string>());
        var seconds = (node["tracks"]?.AsArray() ?? []).Sum(track => track?["duration"]?.GetValue<double>() ?? 0);
        var track = new Track { Key = index.ToString(CultureInfo.InvariantCulture), Title = title, TrackUrl = "yoto:#x", Duration = TimeSpan.FromSeconds(seconds), IconMediaId = icon };
        return new Chapter { Key = track.Key, Title = title, IconMediaId = icon, Tracks = [track] };
    }

    private static Playlist Sample() => new()
    {
        Title = "Bedtime Stories",
        Chapters = SampleTitles
            .Select((title, index) => new Chapter { Key = title, Title = title, Tracks = [new Track { Key = title, Title = title, TrackUrl = "yoto:#x", Duration = TimeSpan.FromMinutes(3 + index) }] })
            .ToList(),
    };
}
