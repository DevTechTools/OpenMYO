using System.Net;

namespace OpenMyo.Yoto.Http;

/// <summary>A Yoto API call failed. Carries the HTTP status and Yoto's error code/message, never tokens.</summary>
public sealed class YotoApiException : Exception
{
    public YotoApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public YotoApiException()
        : base("The Yoto API returned an error.")
    {
    }

    public YotoApiException(string message)
        : base(message)
    {
    }

    public YotoApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpStatusCode StatusCode { get; }

    /// <summary>Yoto's <c>error.code</c> (or OAuth <c>error</c>) when the body had one.</summary>
    public string? ErrorCode { get; }
}
