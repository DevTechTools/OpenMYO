namespace OpenMyo.Core.Abstractions;

/// <summary>Fetches RSS feeds and their episode audio over HTTP.</summary>
public interface IFeedClient
{
    /// <summary>Downloads the feed document as text.</summary>
    Task<string> GetFeedAsync(Uri feedUrl, CancellationToken cancellationToken);

    /// <summary>Streams <paramref name="source"/> to a new file at <paramref name="destinationPath"/>.</summary>
    /// <param name="progress">Receives the fraction of this file done (0 to 1), when the server reports a size.</param>
    Task DownloadAsync(Uri source, string destinationPath, IProgress<double>? progress, CancellationToken cancellationToken);
}
