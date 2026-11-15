using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenMyo.Yoto.Json;

/// <summary>Null-tolerant readers for card documents, which are handled as <see cref="JsonNode"/> to keep unknown fields.</summary>
internal static class JsonNodeReader
{
    public static JsonObject? GetObject(this JsonNode? node, string name) => (node as JsonObject)?[name] as JsonObject;

    public static JsonArray? GetArray(this JsonNode? node, string name) => (node as JsonObject)?[name] as JsonArray;

    /// <summary>Reads a string; numbers are returned as their text so numeric keys still work.</summary>
    public static string? GetString(this JsonNode? node, string name)
    {
        if ((node as JsonObject)?[name] is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<string>(out var text))
        {
            return text;
        }

        return value.GetValueKind() == JsonValueKind.Number ? value.ToJsonString() : null;
    }

    /// <summary>Reads a number that the API may send as an integer or a decimal; 0 when absent.</summary>
    public static double GetNumber(this JsonNode? node, string name) => AsNumber((node as JsonObject)?[name]);

    public static double AsNumber(JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return 0;
        }

        if (value.TryGetValue<double>(out var number) && double.IsFinite(number))
        {
            return number;
        }

        if (value.TryGetValue<long>(out var whole))
        {
            return whole;
        }

        return value.TryGetValue<int>(out var small) ? small : 0;
    }
}
