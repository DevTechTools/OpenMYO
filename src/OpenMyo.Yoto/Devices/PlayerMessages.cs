using System.Globalization;
using System.Text.Json.Nodes;
using OpenMyo.Core.Players;

namespace OpenMyo.Yoto.Devices;

/// <summary>
/// MQTT topics and payloads for Yoto players (see yoto.dev "Players MQTT"). Commands are published to
/// <c>device/{id}/command/...</c>; the player reports on <c>device/{id}/data/events</c>.
/// </summary>
internal static class PlayerMessages
{
    /// <summary>Cards are addressed by their short link, the same one printed on a card's NFC tag.</summary>
    private const string CardUriPrefix = "https://yoto.io/";

    public static string Command(string deviceId, string command) => $"device/{deviceId}/command/{command}";

    public static string Events(string deviceId) => $"device/{deviceId}/data/events";

    /// <summary>Asks the player to publish its current state now; also keeps the connection alive.</summary>
    public static string EventsRequest(string deviceId) => Command(deviceId, "events/request");

    public static string CommandName(PlayerCommand command) => command switch
    {
        PlayerCommand.Pause => "card/pause",
        PlayerCommand.Resume => "card/resume",
        PlayerCommand.Stop => "card/stop",
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null),
    };

    public static string StartPayload(PlayTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new JsonObject
        {
            ["uri"] = CardUriPrefix + target.CardId,
            ["chapterKey"] = target.ChapterKey,
            ["trackKey"] = target.TrackKey,
            ["secondsIn"] = (int)Math.Max(0, target.StartAt.TotalSeconds),
            ["cutOff"] = 0,
            ["anyButtonStop"] = false,
        }.ToJsonString();
    }

    /// <summary>
    /// <c>volume/set</c> takes a percentage, but players report volume in steps (0–16), so the step is converted.
    /// Rounding up makes the player land on the step asked for (it rounds the percentage down to a step).
    /// </summary>
    public static string VolumePayload(int level)
    {
        var step = Math.Clamp(level, 0, PlayerState.VolumeSteps);
        var percent = (int)Math.Ceiling(step * 100.0 / PlayerState.VolumeSteps);
        return new JsonObject { ["volume"] = percent }.ToJsonString();
    }

    /// <summary>Reads an events message. Fields missing from the message stay null, so they can be merged.</summary>
    public static PlayerStateUpdate? ParseEvents(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject message)
        {
            return null;
        }

        return new PlayerStateUpdate(
            CardId: Text(message, "cardId"),
            ChapterKey: Text(message, "chapterKey"),
            TrackKey: Text(message, "trackKey"),
            TrackTitle: Text(message, "trackTitle"),
            Position: Seconds(message, "position"),
            Length: Seconds(message, "trackLength"),
            Status: Status(Text(message, "playbackStatus")),
            Volume: Number(message, "volume") is { } volume ? (int)volume : null,
            VolumeMax: Number(message, "volumeMax") is { } max ? (int)max : null);
    }

    private static string? Text(JsonObject message, string name) =>
        message[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text
        : Number(message, name)?.ToString(CultureInfo.InvariantCulture);

    private static double? Number(JsonObject message, string name) =>
        message[name] is JsonValue value && value.TryGetValue<double>(out var number) && double.IsFinite(number) ? number : null;

    private static TimeSpan? Seconds(JsonObject message, string name) =>
        Number(message, name) is { } seconds ? TimeSpan.FromSeconds(seconds) : null;

    private static PlayerStatus? Status(string? text) => text?.ToUpperInvariant() switch
    {
        null => null,
        "PLAYING" => PlayerStatus.Playing,
        "PAUSED" => PlayerStatus.Paused,
        "STOPPED" => PlayerStatus.Stopped,
        _ => PlayerStatus.Unknown,
    };
}
