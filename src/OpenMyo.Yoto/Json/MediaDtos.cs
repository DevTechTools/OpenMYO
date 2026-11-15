using System.Text.Json.Nodes;

namespace OpenMyo.Yoto.Json;

internal sealed record UploadUrlResponse
{
    public UploadTicket? Upload { get; init; }
}

internal sealed record UploadTicket
{
    public string? UploadId { get; init; }

    /// <summary>Null when Yoto already has a file with the same hash.</summary>
    public string? UploadUrl { get; init; }
}

internal sealed record TranscodeResponse
{
    public TranscodeResult? Transcode { get; init; }
}

internal sealed record TranscodeResult
{
    public string? TranscodedSha256 { get; init; }

    public TranscodedInfo? TranscodedInfo { get; init; }
}

internal sealed record TranscodedInfo
{
    public double? Duration { get; init; }

    public double? FileSize { get; init; }

    /// <summary>Text (<c>stereo</c>) or a channel count depending on the endpoint.</summary>
    public JsonNode? Channels { get; init; }

    public string? Format { get; init; }
}

internal sealed record CoverUploadResponse
{
    public CoverImage? CoverImage { get; init; }
}

internal sealed record CoverImage
{
    public string? MediaId { get; init; }

    public string? MediaUrl { get; init; }
}

internal sealed record IconUploadResponse
{
    public UploadedIcon? DisplayIcon { get; init; }
}

internal sealed record UploadedIcon
{
    public string? MediaId { get; init; }
}
