namespace OpenMyo.Yoto.Http;

internal static class RequestContent
{
    /// <summary>
    /// True when the request can be sent a second time. Buffered content (strings, byte arrays) can be
    /// replayed; streamed content such as file uploads cannot.
    /// </summary>
    public static bool CanResend(HttpRequestMessage request) =>
        request.Content is null or ByteArrayContent;
}
