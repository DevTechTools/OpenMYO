using System.Text.Json.Nodes;

namespace OpenMyo.Yoto.Tests.Content;

internal static class Samples
{
    public const string IconA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    public const string IconB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    public static JsonObject Card() => JsonNode.Parse($$$"""
        {
          "cardId": "abc12",
          "title": "Bedtime",
          "updatedAt": "2025-01-02T03:04:05.000Z",
          "createdAt": "2025-01-01T00:00:00.000Z",
          "userId": "u1",
          "extraCardField": {"keep": true},
          "content": {
            "config": {"resumeTimeout": 2592000},
            "chapters": [
              {"key": "01", "title": "One", "overlayLabel": "1", "chapterOnly": 7, "duration": 10, "fileSize": 100,
               "tracks": [{"key": "01", "title": "One", "trackUrl": "yoto:#sha1", "format": "mp3", "type": "audio",
                           "duration": 10.5, "fileSize": 100, "channels": "stereo", "trackOnly": "x"}]},
              {"key": "02", "title": "Two", "display": {"icon16x16": "yoto:#{{{IconA}}}"},
               "tracks": [{"key": "01", "title": "Two", "trackUrl": "yoto:#sha2", "duration": 20, "fileSize": 200, "channels": 1,
                           "display": {"icon16x16": "yoto:#{{{IconB}}}"}}]}
            ]
          },
          "metadata": {"cover": {"imageL": "https://example.com/c.png"}, "media": {"duration": 30, "fileSize": 300}, "other": 1}
        }
        """)!.AsObject();
}
