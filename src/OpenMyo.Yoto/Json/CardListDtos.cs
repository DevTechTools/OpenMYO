namespace OpenMyo.Yoto.Json;

internal sealed record CardListResponse
{
    public IReadOnlyList<CardListItem>? Cards { get; init; }
}

internal sealed record CardListItem
{
    public string? CardId { get; init; }

    public string? Title { get; init; }

    public string? UpdatedAt { get; init; }

    public CardListMetadata? Metadata { get; init; }
}

internal sealed record CardListMetadata
{
    public CardListCover? Cover { get; init; }
}

internal sealed record CardListCover
{
    public string? ImageL { get; init; }
}
