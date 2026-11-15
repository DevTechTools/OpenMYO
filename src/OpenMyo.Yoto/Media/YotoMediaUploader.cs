using System.Net;
using System.Security.Cryptography;
using OpenMyo.Core.Abstractions;
using OpenMyo.Yoto.Content;
using OpenMyo.Yoto.Http;
using OpenMyo.Yoto.Icons;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Media;

/// <summary>Uploads audio, covers and icons to Yoto.</summary>
internal sealed class YotoMediaUploader(
    IHttpClientFactory httpClientFactory,
    YotoIconCatalog iconCatalog,
    PollingPolicy? polling = null) : IMediaUploader
{
    /// <summary>Upload takes this share of the progress bar; transcoding the rest.</summary>
    private const double UploadProgressShare = 0.9;
    private const int MaxIconNameLength = 100;

    private readonly PollingPolicy _polling = polling ?? PollingPolicy.Default;

    public async Task<UploadedAudio> UploadAudioAsync(string filePath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var hash = await ComputeSha256Async(filePath, cancellationToken);
        var ticket = await RequestUploadAsync(hash, Path.GetFileName(filePath), cancellationToken);
        if (ticket.UploadUrl is not null)
        {
            await PutFileAsync(ticket.UploadUrl, filePath, progress, cancellationToken);
        }

        var result = await WaitForTranscodeAsync(ticket.UploadId!, cancellationToken); // RequestUploadAsync guarantees an id.
        progress?.Report(1);
        var info = result.TranscodedInfo;
        return new UploadedAudio(
            "yoto:#" + result.TranscodedSha256,
            info?.Format ?? "aac",
            CardMapper.FromSeconds(info?.Duration ?? 0),
            (long)(info?.FileSize ?? 0),
            CardMapper.ChannelsText(info?.Channels));
    }

    public async Task<Uri> UploadCoverAsync(ReadOnlyMemory<byte> image, string contentType, CancellationToken cancellationToken)
    {
        var response = await PostImageAsync("media/coverImage/user/me/upload?autoconvert=true&coverType=myo", image, contentType, cancellationToken);
        var parsed = await YotoResponse.ReadAsync(response, YotoJsonContext.Default.CoverUploadResponse, cancellationToken);
        return Uri.TryCreate(parsed.CoverImage?.MediaUrl, UriKind.Absolute, out var url)
            ? url
            : throw new YotoApiException(response.StatusCode, null, "Yoto did not return a cover image URL.");
    }

    public async Task<string> UploadIconAsync(ReadOnlyMemory<byte> image, string contentType, string fileName, CancellationToken cancellationToken)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        name = name.Length > MaxIconNameLength ? name[..MaxIconNameLength] : name;
        var path = $"media/displayIcons/user/me/upload?autoConvert=true&filename={Uri.EscapeDataString(name)}";
        var response = await PostImageAsync(path, image, contentType, cancellationToken);
        var parsed = await YotoResponse.ReadAsync(response, YotoJsonContext.Default.IconUploadResponse, cancellationToken);
        var mediaId = parsed.DisplayIcon?.MediaId
            ?? throw new YotoApiException(response.StatusCode, null, "Yoto did not return an icon id.");
        iconCatalog.InvalidateUserIcons();
        return mediaId;
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(file, cancellationToken));
    }

    private async Task<UploadTicket> RequestUploadAsync(string sha256, string fileName, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        var path = $"media/transcode/audio/uploadUrl?sha256={sha256}&filename={Uri.EscapeDataString(fileName)}";
        using var response = await client.GetAsync(path, cancellationToken);
        var parsed = await YotoResponse.ReadAsync(response, YotoJsonContext.Default.UploadUrlResponse, cancellationToken);
        return parsed.Upload is { UploadId: not null } ticket
            ? ticket
            : throw new YotoApiException(response.StatusCode, null, "Yoto did not return an upload id.");
    }

    private async Task PutFileAsync(string uploadUrl, string filePath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // Presigned URL: the upload client has no auth handler so the bearer token is never sent to storage.
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Upload);
        using var content = new ProgressFileContent(
            filePath,
            AudioContentTypes.ForFile(filePath),
            fraction => progress?.Report(fraction * UploadProgressShare));
        using var response = await client.PutAsync(new Uri(uploadUrl), content, cancellationToken);
        await YotoResponse.EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<TranscodeResult> WaitForTranscodeAsync(string uploadId, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        var path = $"media/upload/{Uri.EscapeDataString(uploadId)}/transcoded?loudnorm=false";
        var delay = _polling.InitialDelay;
        var deadline = DateTimeOffset.UtcNow + _polling.Timeout;
        while (true)
        {
            using var response = await client.GetAsync(path, cancellationToken);
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                var parsed = await YotoResponse.ReadAsync(response, YotoJsonContext.Default.TranscodeResponse, cancellationToken);
                if (parsed.Transcode is { TranscodedSha256.Length: > 0 } done)
                {
                    return done;
                }
            }

            if (DateTimeOffset.UtcNow + delay > deadline)
            {
                throw new TimeoutException("Yoto took too long to process the audio file. Try uploading it again.");
            }

            await Task.Delay(delay, cancellationToken);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _polling.MaxDelay.Ticks));
        }
    }

    private async Task<HttpResponseMessage> PostImageAsync(string path, ReadOnlyMemory<byte> image, string contentType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var content = new ByteArrayContent(image.ToArray());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return await client.PostAsync(path, content, cancellationToken);
    }
}
