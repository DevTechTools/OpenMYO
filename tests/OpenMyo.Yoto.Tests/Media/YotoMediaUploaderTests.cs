using System.Net;
using OpenMyo.Yoto.Icons;
using OpenMyo.Yoto.Media;
using OpenMyo.Yoto.Tests.Support;

namespace OpenMyo.Yoto.Tests.Media;

public sealed class YotoMediaUploaderTests : IDisposable
{
    // SHA-256 of the ASCII bytes "hello".
    private const string HelloSha = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";
    private const string Pending = """{"transcode":{}}""";
    private const string Done = """
        {"transcode":{"transcodedSha256":"tsha","transcodedInfo":{"duration":12.5,"fileSize":2048,"channels":"stereo","format":"aac"}}}
        """;

    private static readonly PollingPolicy FastPolling = new(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2), TimeSpan.FromSeconds(5));

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"openmyo-{Guid.NewGuid():N}.mp3");

    public YotoMediaUploaderTests() => File.WriteAllText(_file, "hello");

    public void Dispose() => File.Delete(_file);

    private static YotoMediaUploader Uploader(StubHttpHandler api, StubHttpHandler? upload = null) =>
        new(new StubHttpClientFactory(api, upload), new YotoIconCatalog(new StubHttpClientFactory(api), new TempPaths()), FastPolling);

    private string UploadRoute => $"GET /media/transcode/audio/uploadUrl?sha256={HelloSha}&filename={Uri.EscapeDataString(Path.GetFileName(_file))}";

    [Fact]
    public async Task UploadAudioAsync_NewFile_PutsWithoutAuthThenPollsUntilTranscoded()
    {
        var api = new StubHttpHandler()
            .On(UploadRoute, """{"upload":{"uploadId":"u1","uploadUrl":"https://s3.example.com/put"}}""")
            .On("GET /media/upload/u1/transcoded?loudnorm=false", Pending)
            .On("GET /media/upload/u1/transcoded?loudnorm=false", Pending)
            .On("GET /media/upload/u1/transcoded?loudnorm=false", Done);
        var upload = new StubHttpHandler().On("PUT /put", "{}");
        var progress = new List<double>();

        var result = await Uploader(api, upload).UploadAudioAsync(_file, new SyncProgress(progress), CancellationToken.None);

        Assert.Equal("yoto:#tsha", result.TrackUrl);
        Assert.Equal("aac", result.Format);
        Assert.Equal(TimeSpan.FromSeconds(12.5), result.Duration);
        Assert.Equal(2048, result.FileSize);
        Assert.Equal("stereo", result.Channels);
        Assert.Single(upload.Requests);
        Assert.Null(upload.Sent[0].Headers.Authorization);
        Assert.Equal("audio/mpeg", upload.Sent[0].Content!.Headers.ContentType!.MediaType);
        Assert.True(api.Requests.Count(r => r.Contains("transcoded", StringComparison.Ordinal)) >= 3);
        Assert.Equal(1, progress[^1]);
    }

    [Fact]
    public async Task UploadAudioAsync_UploadUrlNull_SkipsPut()
    {
        var api = new StubHttpHandler()
            .On(UploadRoute, """{"upload":{"uploadId":"u1","uploadUrl":null}}""")
            .On("GET /media/upload/u1/transcoded?loudnorm=false", Done);
        var upload = new StubHttpHandler();

        var result = await Uploader(api, upload).UploadAudioAsync(_file, null, CancellationToken.None);

        Assert.Equal("yoto:#tsha", result.TrackUrl);
        Assert.Empty(upload.Requests);
    }

    [Fact]
    public async Task UploadAudioAsync_NeverTranscodes_ThrowsTimeout()
    {
        var api = new StubHttpHandler()
            .On(UploadRoute, """{"upload":{"uploadId":"u1","uploadUrl":null}}""")
            .On("GET /media/upload/u1/transcoded?loudnorm=false", Pending);
        var tinyTimeout = new YotoMediaUploader(
            new StubHttpClientFactory(api),
            new YotoIconCatalog(new StubHttpClientFactory(api), new TempPaths()),
            new PollingPolicy(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(30)));

        await Assert.ThrowsAsync<TimeoutException>(() => tinyTimeout.UploadAudioAsync(_file, null, CancellationToken.None));
    }

    [Fact]
    public async Task UploadIconAsync_Success_ReturnsMediaIdAndUsesBaseName()
    {
        var api = new StubHttpHandler().On(
            "POST /media/displayIcons/user/me/upload?autoConvert=true&filename=my%20icon",
            """{"displayIcon":{"displayIconId":"d","mediaId":"newmedia"}}""");

        var id = await Uploader(api).UploadIconAsync(new byte[] { 1, 2 }, "image/png", "my icon.png", CancellationToken.None);

        Assert.Equal("newmedia", id);
    }

    [Fact]
    public async Task UploadCoverAsync_Success_ReturnsMediaUrl()
    {
        var api = new StubHttpHandler().On(
            "POST /media/coverImage/user/me/upload?autoconvert=true&coverType=myo",
            """{"coverImage":{"mediaId":"m","mediaUrl":"https://cdn.example.com/c.png"}}""");

        var url = await Uploader(api).UploadCoverAsync(new byte[] { 1 }, "image/jpeg", CancellationToken.None);

        Assert.Equal("https://cdn.example.com/c.png", url.AbsoluteUri);
    }

    [Fact]
    public async Task UploadCoverAsync_ErrorBody_ThrowsApiExceptionWithMessage()
    {
        var api = new StubHttpHandler().On(
            "POST /media/coverImage/user/me/upload?autoconvert=true&coverType=myo",
            """{"error":{"code":"bad_image","message":"Image too small"}}""",
            HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<OpenMyo.Yoto.Http.YotoApiException>(
            () => Uploader(api).UploadCoverAsync(new byte[] { 1 }, "image/png", CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("bad_image", ex.ErrorCode);
        Assert.Contains("Image too small", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Synchronous progress so assertions do not race the thread-pool callbacks of <see cref="Progress{T}"/>.</summary>
    private sealed class SyncProgress(List<double> values) : IProgress<double>
    {
        public void Report(double value) => values.Add(value);
    }
}
