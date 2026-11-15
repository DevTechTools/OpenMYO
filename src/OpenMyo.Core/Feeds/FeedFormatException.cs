namespace OpenMyo.Core.Feeds;

/// <summary>The address did not return a usable RSS feed. The message is written for the user.</summary>
public sealed class FeedFormatException : Exception
{
    public FeedFormatException()
    {
    }

    public FeedFormatException(string message)
        : base(message)
    {
    }

    public FeedFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
