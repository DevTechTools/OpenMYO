using System.Globalization;
using System.Text.Json.Nodes;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Content;

/// <summary>
/// Builds the <c>chapters</c> array: existing chapters and tracks are cloned from the last-fetched card
/// (so fields this app does not model survive), new ones are created, and keys are renumbered.
/// </summary>
internal static class ChapterComposer
{
    public static JsonArray Compose(JsonArray? rawChapters, IReadOnlyList<Chapter> chapters)
    {
        var rawByKey = IndexByKey(rawChapters);
        var result = new JsonArray();
        var tracksBefore = 0;
        for (var index = 0; index < chapters.Count; index++)
        {
            result.Add(ComposeChapter(rawByKey, chapters[index], index, tracksBefore));
            tracksBefore += chapters[index].Tracks.Count;
        }

        return result;
    }

    private static JsonObject ComposeChapter(Dictionary<string, JsonObject> rawByKey, Chapter chapter, int index, int tracksBefore)
    {
        var node = Clone(rawByKey, chapter.Key) ?? new JsonObject();
        node["key"] = KeyFor(index);
        node["title"] = chapter.Title;
        node["overlayLabel"] = LabelFor(index);
        SetIcon(node, chapter.IconMediaId);

        var rawTracks = IndexByKey(node["tracks"] as JsonArray);
        var tracks = new JsonArray();
        for (var trackIndex = 0; trackIndex < chapter.Tracks.Count; trackIndex++)
        {
            tracks.Add(ComposeTrack(rawTracks, chapter.Tracks[trackIndex], trackIndex, tracksBefore + trackIndex));
        }

        node["tracks"] = tracks;
        node["duration"] = Math.Round(chapter.Duration.TotalSeconds);
        node["fileSize"] = chapter.Tracks.Sum(track => track.FileSize);
        return node;
    }

    /// <summary>
    /// Track keys restart at "01" in each chapter, but <c>overlayLabel</c> counts tracks across the whole card
    /// (as in Yoto's own examples); restarting it per chapter made every single-track chapter show "1".
    /// </summary>
    private static JsonObject ComposeTrack(Dictionary<string, JsonObject> rawByKey, Track track, int index, int cardTrackIndex)
    {
        var node = Clone(rawByKey, track.Key);
        var isNew = node is null;
        node ??= new JsonObject();
        var urlChanged = isNew || !string.Equals(node.GetString("trackUrl"), track.TrackUrl, StringComparison.Ordinal);

        node["key"] = KeyFor(index);
        node["title"] = track.Title;
        node["overlayLabel"] = LabelFor(cardTrackIndex);
        node["trackUrl"] = track.TrackUrl;
        SetIcon(node, track.IconMediaId);
        if (urlChanged)
        {
            SetMedia(node, track);
        }

        return node;
    }

    private static void SetMedia(JsonObject node, Track track)
    {
        node["type"] = track.Type;
        node["format"] = track.Format;
        node["duration"] = Math.Round(track.Duration.TotalSeconds);
        node["fileSize"] = track.FileSize;
        if (track.Channels is not null)
        {
            node["channels"] = track.Channels;
        }
    }

    private static void SetIcon(JsonObject node, string? mediaId)
    {
        var display = node["display"] as JsonObject;
        if (!CardIds.IsValidMediaId(mediaId))
        {
            display?.Remove("icon16x16");
            return;
        }

        if (display is null)
        {
            display = new JsonObject();
            node["display"] = display;
        }

        display["icon16x16"] = DisplayIcon.ToYotoUri(mediaId!); // IsValidMediaId rules out null.
    }

    /// <summary>Removes the match so two domain items never clone the same raw item.</summary>
    private static JsonObject? Clone(Dictionary<string, JsonObject> rawByKey, string key) =>
        rawByKey.Remove(key, out var raw) ? raw.DeepClone().AsObject() : null;

    private static Dictionary<string, JsonObject> IndexByKey(JsonArray? array)
    {
        var index = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var item in array?.OfType<JsonObject>() ?? [])
        {
            if (item.GetString("key") is { } key)
            {
                index.TryAdd(key, item);
            }
        }

        return index;
    }

    private static string KeyFor(int index) => (index + 1).ToString("D2", CultureInfo.InvariantCulture);

    private static string LabelFor(int index) => (index + 1).ToString(CultureInfo.InvariantCulture);
}
