using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;
using OpenMyo.Mcp.Icons;
using OpenMyo.Mcp.Players;
using OpenMyo.Mcp.Playlists;
using OpenMyo.Mcp.Support;
using OpenMyo.Mcp.Tests.Fakes;

namespace OpenMyo.Mcp.Tests;

/// <summary>Wires the real tool classes over in-memory fakes, like the app does with real services.</summary>
public sealed class McpWorld
{
    public const string CardId = "card-1";

    public McpWorld(IReadOnlyList<DisplayIcon>? icons = null, IReadOnlyList<CommunityIcon>? community = null)
    {
        Repository = new FakePlaylistRepository(SamplePlaylist());
        var indexProvider = new IconIndexProvider(new StubIconCatalog(icons ?? DefaultIcons()));
        var catalog = new CommunityCatalog(community ?? [], DateTimeOffset.UtcNow, IsComplete: true);
        var source = new StubCommunitySource();
        var provider = new CommunityCatalogProvider(new CommunityCatalogHarvester(source), new StubCatalogCache(catalog));
        var search = new CommunityIconSearch(provider);
        var importer = new CommunityIconImporter(source, Uploader, indexProvider);
        var lookup = new PlaylistLookup(Repository);
        var labeler = new IconLabeler(indexProvider);
        Playlists = new PlaylistTools(Repository, lookup, labeler);
        PlaylistEdit = new PlaylistEditTools(lookup);
        Tracks = new TrackEditTools(lookup);
        TrackSearch = new TrackSearchTools(Repository);
        Players = new PlayerTools(PlayerDirectory, Remote, lookup);
        Search = new IconSearchTools(indexProvider, search);
        Edit = new IconEditTools(lookup, new IconResolver(indexProvider, search, importer), labeler);
    }

    public FakePlaylistRepository Repository { get; }

    public RecordingUploader Uploader { get; } = new();

    public PlaylistTools Playlists { get; }

    public PlaylistEditTools PlaylistEdit { get; }

    public TrackEditTools Tracks { get; }

    public TrackSearchTools TrackSearch { get; }

    public PlayerTools Players { get; }

    public FakePlayerDirectory PlayerDirectory { get; } = new(
        new OpenMyo.Core.Players.YotoPlayer("d1", "Kitchen", IsOnline: true),
        new OpenMyo.Core.Players.YotoPlayer("d2", "Bedroom", IsOnline: false));

    public RecordingPlayerRemote Remote { get; } = new();

    public IconSearchTools Search { get; }

    public IconEditTools Edit { get; }

    public static DisplayIcon Icon(string id, string title) => new(id, title, [], null, IsPublic: true);

    public static CommunityIcon Community(string id, string title) =>
        new(id, title, title.Split(' '), "artist", 10, new Uri($"https://yotoicons.com/{id}.png"));

    private static List<DisplayIcon> DefaultIcons() => [Icon("rabbit-id", "rabbit"), Icon("moon-id", "moon")];

    private static Playlist SamplePlaylist() => new()
    {
        CardId = CardId,
        Title = "Bedtime",
        Chapters =
        [
            Chapter("1", "Chapter 1 - The Rabbit", "moon-id"),
            Chapter("2", "Chapter 2 - The Moon", null),
        ],
    };

    private static Chapter Chapter(string key, string title, string? icon) => new()
    {
        Key = key,
        Title = title,
        IconMediaId = icon,
        Tracks = [new Track { Key = "t" + key, Title = title, TrackUrl = "yoto:#x", IconMediaId = icon }],
    };
}
