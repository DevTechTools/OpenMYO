namespace OpenMyo.Yoto.Content;

/// <summary>Format checks for ids that end up in URLs or card JSON.</summary>
internal static class CardIds
{
    private const int MaxCardIdLength = 64;
    private const int MediaIdLength = 43;

    /// <summary>Card ids are short alphanumeric strings; anything else must not reach a URL.</summary>
    public static bool IsValidCardId(string? cardId) =>
        !string.IsNullOrEmpty(cardId) && cardId.Length <= MaxCardIdLength && cardId.All(char.IsAsciiLetterOrDigit);

    /// <summary>Yoto only accepts <c>yoto:#</c> icon references with a 43 character URL-safe media id.</summary>
    public static bool IsValidMediaId(string? mediaId) =>
        mediaId is { Length: MediaIdLength } && mediaId.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

    public static string RequireCardId(string? cardId) =>
        IsValidCardId(cardId) ? cardId! : throw new ArgumentException("Invalid Yoto card id.", nameof(cardId)); // IsValidCardId rules out null.
}
