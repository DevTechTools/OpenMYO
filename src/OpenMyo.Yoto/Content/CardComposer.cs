using System.Text.Json.Nodes;
using OpenMyo.Core.Playlists;
using OpenMyo.Core.Text;

namespace OpenMyo.Yoto.Content;

/// <summary>Builds the body for <c>POST /content</c> by patching the last-fetched card, never by rebuilding it.</summary>
internal static class CardComposer
{
    /// <summary>Server-managed fields that are not part of what clients write.</summary>
    private static readonly string[] ReadOnlyFields = ["createdAt", "updatedAt", "_id", "userId"];

    public static JsonObject Compose(JsonObject? rawCard, Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        var title = playlist.Title.Trim();
        if (title.Length is 0 or > TitleCleaner.MaxTitleLength)
        {
            throw new ArgumentException($"A playlist title must be 1-{TitleCleaner.MaxTitleLength} characters.", nameof(playlist));
        }

        ThrowIfAnyChapterTitleTooLong(playlist.Chapters);

        var card = rawCard?.DeepClone().AsObject() ?? new JsonObject();
        foreach (var field in ReadOnlyFields)
        {
            card.Remove(field);
        }

        card["title"] = title;
        if (playlist.CardId is not null)
        {
            card["cardId"] = playlist.CardId;
        }

        var content = GetOrAddObject(card, "content");
        content["chapters"] = ChapterComposer.Compose(content["chapters"] as JsonArray, playlist.Chapters);

        var metadata = GetOrAddObject(card, "metadata");
        var media = GetOrAddObject(metadata, "media");
        media["duration"] = Math.Round(playlist.Duration.TotalSeconds);
        media["fileSize"] = playlist.FileSize;
        if (playlist.CoverImageUrl is not null)
        {
            GetOrAddObject(metadata, "cover")["imageL"] = playlist.CoverImageUrl.AbsoluteUri;
        }

        return card;
    }

    private static JsonObject GetOrAddObject(JsonObject parent, string name)
    {
        if (parent[name] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[name] = created;
        return created;
    }

    // Yoto rejects the whole save for one over-long chapter title, with an error that doesn't say which.
    private static void ThrowIfAnyChapterTitleTooLong(IReadOnlyList<Chapter> chapters)
    {
        var tooLong = chapters.Select((chapter, index) => (chapter, number: index + 1))
            .Where(item => item.chapter.Title.Length > TitleCleaner.MaxTitleLength)
            .Select(item => item.number)
            .ToList();
        if (tooLong.Count > 0)
        {
            throw new ArgumentException(
                $"Track(s) {string.Join(", ", tooLong)} have titles longer than {TitleCleaner.MaxTitleLength} characters.", nameof(chapters));
        }
    }
}
