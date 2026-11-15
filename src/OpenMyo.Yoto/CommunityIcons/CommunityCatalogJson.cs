using System.Text.Json.Serialization;

namespace OpenMyo.Yoto.CommunityIcons;

/// <summary>On-disk shape of the harvested catalog; kept separate from Core's records so the file format is explicit.</summary>
internal sealed record CommunityCatalogFileDto(DateTimeOffset FetchedAt, bool IsComplete, IReadOnlyList<CommunityIconDto> Icons);

internal sealed record CommunityIconDto(string Id, string Title, IReadOnlyList<string> Tags, string? Artist, int Downloads, string ImageUrl);

/// <summary>Source-generated serializer metadata for the catalog file (trim and AOT friendly).</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommunityCatalogFileDto))]
internal sealed partial class CommunityCatalogJsonContext : JsonSerializerContext
{
}
