using System.Xml;
using System.Xml.Linq;
using OpenMyo.Core.Import;

namespace OpenMyo.Core.Feeds;

/// <summary>
/// Reads an RSS 2.0 feed (including the iTunes podcast extensions) into a <see cref="PodcastFeed"/>.
/// Only items with an audio enclosure become episodes, ordered oldest first.
/// </summary>
public static class FeedParser
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    public static PodcastFeed Parse(string xml, Uri feedUrl)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(feedUrl);

        var channel = Load(xml).Root is { Name.LocalName: "rss" } rss ? rss.Element("channel") : null;
        if (channel is null)
        {
            throw new FeedFormatException("That address isn't an RSS feed. Check that it's the feed link, not the podcast's web page.");
        }

        var items = channel.Elements("item").ToList();
        var episodes = items.Select((item, i) => ReadEpisode(item, feedUrl, items.Count - i)).OfType<FeedEpisode>().ToList();
        if (episodes.Count == 0)
        {
            throw new FeedFormatException("This feed has no audio episodes to import.");
        }

        var title = Text(channel.Element("title"));
        return new PodcastFeed(
            feedUrl,
            string.IsNullOrEmpty(title) ? feedUrl.Host : title,
            ResolveUrl(channel.Element(Itunes + "image")?.Attribute("href")?.Value, feedUrl)
                ?? ResolveUrl(channel.Element("image")?.Element("url")?.Value, feedUrl),
            ListeningOrder(episodes));
    }

    private static XDocument Load(string xml)
    {
        // Feeds come from arbitrary servers: never resolve DTDs or external entities.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            return XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new FeedFormatException("That address didn't return a readable RSS feed.", exception);
        }
    }

    /// <param name="fallbackNumber">Used for untitled items: feeds list newest first, so the last item is episode 1.</param>
    private static FeedEpisode? ReadEpisode(XElement item, Uri feedUrl, int fallbackNumber)
    {
        var enclosure = item.Element("enclosure");
        var audioUrl = ResolveUrl(enclosure?.Attribute("url")?.Value, feedUrl);
        var contentType = enclosure?.Attribute("type")?.Value.Trim();
        if (audioUrl is null || !IsAudio(audioUrl, contentType))
        {
            return null;
        }

        var title = Text(item.Element("title"));
        return new FeedEpisode(
            string.IsNullOrEmpty(title) ? $"Episode {fallbackNumber}" : title,
            audioUrl,
            FeedValues.ParseDate(item.Element("pubDate")?.Value),
            FeedValues.ParseDuration(item.Element(Itunes + "duration")?.Value),
            string.IsNullOrEmpty(contentType) ? null : contentType);
    }

    private static bool IsAudio(Uri url, string? contentType) => string.IsNullOrEmpty(contentType)
        ? MediaTypes.IsAudio(url.AbsolutePath)
        : contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

    /// <summary>By publish date when every episode has one; otherwise reverse feed order, since feeds list newest first.</summary>
    private static List<FeedEpisode> ListeningOrder(List<FeedEpisode> episodes) =>
        episodes.TrueForAll(episode => episode.Published is not null)
            ? episodes.OrderBy(episode => episode.Published).ToList()
            : Enumerable.Reverse(episodes).ToList();

    private static Uri? ResolveUrl(string? value, Uri feedUrl) =>
        Uri.TryCreate(feedUrl, value?.Trim(), out var url) && FeedUrl.IsWeb(url) ? url : null;

    private static string Text(XElement? element) =>
        string.Join(' ', (element?.Value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
