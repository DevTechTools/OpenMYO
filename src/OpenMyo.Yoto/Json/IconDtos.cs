namespace OpenMyo.Yoto.Json;

internal sealed record IconListResponse
{
    public IReadOnlyList<IconItem>? DisplayIcons { get; init; }
}

internal sealed record IconItem
{
    public string? MediaId { get; init; }

    public string? Title { get; init; }

    public IReadOnlyList<string>? PublicTags { get; init; }

    public string? Url { get; init; }
}
