using System.Globalization;
using System.Text.Json.Nodes;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Content;

/// <summary>Maps Yoto card JSON to domain models. Pure; reading never mutates the document.</summary>
internal static class CardMapper
{
    private const string DefaultFormat = "aac";
    private const string DefaultType = "audio";

    public static PlaylistSummary? ToSummary(CardListItem item)
    {
        if (!CardIds.IsValidCardId(item.CardId))
        {
            return null;
        }

        return new PlaylistSummary(item.CardId!, item.Title ?? string.Empty, ParseDate(item.UpdatedAt), ParseUri(item.Metadata?.Cover?.ImageL));
    }

    public static Playlist ToPlaylist(JsonObject card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var chapters = card.GetObject("content").GetArray("chapters")?.OfType<JsonObject>() ?? [];
        return new Playlist
        {
            CardId = card.GetString("cardId"),
            Title = card.GetString("title") ?? string.Empty,
            CoverImageUrl = ParseUri(card.GetObject("metadata").GetObject("cover").GetString("imageL")),
            UpdatedAt = ParseDate(card.GetString("updatedAt")),
            Chapters = chapters.Select(ToChapter).ToList(),
        };
    }

    public static DateTimeOffset? ParseDate(string? text) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? date : null;

    public static TimeSpan FromSeconds(double seconds) =>
        double.IsFinite(seconds) && seconds > 0 ? TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.MaxValue.TotalSeconds / 2)) : TimeSpan.Zero;

    /// <summary>The API reports channels as text ("stereo") or a count; the domain uses text.</summary>
    public static string? ChannelsText(JsonNode? node) => node is JsonValue value
        ? value.TryGetValue<string>(out var text) ? text : JsonNodeReader.AsNumber(node) switch { 1 => "mono", 2 => "stereo", _ => null }
        : null;

    private static Chapter ToChapter(JsonObject chapter, int index)
    {
        var tracks = (chapter.GetArray("tracks")?.OfType<JsonObject>() ?? []).Select(ToTrack).ToList();
        var iconId = DisplayIcon.MediaIdFromUri(chapter.GetObject("display").GetString("icon16x16"))
            ?? tracks.Select(track => track.IconMediaId).FirstOrDefault(id => id is not null);
        return new Chapter
        {
            Key = chapter.GetString("key") ?? (index + 1).ToString("D2", CultureInfo.InvariantCulture),
            Title = chapter.GetString("title") ?? string.Empty,
            IconMediaId = iconId,
            Tracks = tracks,
        };
    }

    private static Track ToTrack(JsonObject track, int index) => new()
    {
        Key = track.GetString("key") ?? (index + 1).ToString("D2", CultureInfo.InvariantCulture),
        Title = track.GetString("title") ?? string.Empty,
        TrackUrl = track.GetString("trackUrl") ?? string.Empty,
        Format = track.GetString("format") ?? DefaultFormat,
        Type = track.GetString("type") ?? DefaultType,
        Duration = FromSeconds(track.GetNumber("duration")),
        FileSize = (long)track.GetNumber("fileSize"),
        Channels = ChannelsText(track["channels"]),
        IconMediaId = DisplayIcon.MediaIdFromUri(track.GetObject("display").GetString("icon16x16")),
    };

    private static Uri? ParseUri(string? text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) ? uri : null;
}
