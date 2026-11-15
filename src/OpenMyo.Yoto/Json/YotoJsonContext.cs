using System.Text.Json.Serialization;

namespace OpenMyo.Yoto.Json;

/// <summary>Source-generated serializer metadata for the small response DTOs (trim and AOT friendly).</summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(CardListResponse))]
[JsonSerializable(typeof(IconListResponse))]
[JsonSerializable(typeof(UploadUrlResponse))]
[JsonSerializable(typeof(TranscodeResponse))]
[JsonSerializable(typeof(CoverUploadResponse))]
[JsonSerializable(typeof(IconUploadResponse))]
internal sealed partial class YotoJsonContext : JsonSerializerContext
{
}
