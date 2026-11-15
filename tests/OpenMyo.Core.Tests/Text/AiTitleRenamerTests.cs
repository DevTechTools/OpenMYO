using OpenMyo.Core.Playlists;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class AiTitleRenamerTests
{
    [Fact]
    public async Task RenameAll_Suggestions_RenamesChapterAndTrackAndCountsChanges()
    {
        var writer = new StubWriter(["The Gruffalo", "Room on the Broom"]);

        var result = await new AiTitleRenamer(writer).RenameAllAsync(Playlist("03_the_gruffalo", "Room on the Broom"), CancellationToken.None);

        Assert.Equal("03 The Gruffalo", result.Chapters[0].Title);
        Assert.Equal("03 The Gruffalo", result.Chapters[0].Tracks[0].Title);
        Assert.Equal("Room on the Broom", result.Chapters[1].Title);
        Assert.Equal(1, result.ChangedCount);
        Assert.Equal("Stories", writer.PlaylistTitle);
        Assert.Equal(["03_the_gruffalo", "Room on the Broom"], writer.Titles);
    }

    [Fact]
    public async Task RenameAll_BlankOrMissingSuggestion_KeepsOriginal()
    {
        var writer = new StubWriter(["   "]);

        var result = await new AiTitleRenamer(writer).RenameAllAsync(Playlist("One", "Two"), CancellationToken.None);

        Assert.Equal(["One", "Two"], result.Chapters.Select(chapter => chapter.Title));
        Assert.Equal(0, result.ChangedCount);
    }

    [Fact]
    public async Task RenameAll_TooLongSuggestion_IsCutToYotoLimit()
    {
        var writer = new StubWriter([new string('a', TitleCleaner.MaxTitleLength + 20)]);

        var result = await new AiTitleRenamer(writer).RenameAllAsync(Playlist("One"), CancellationToken.None);

        Assert.Equal(TitleCleaner.MaxTitleLength, result.Chapters[0].Title.Length);
    }

    [Fact]
    public async Task RenameAll_NoWriter_Throws() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AiTitleRenamer().RenameAllAsync(Playlist("One"), CancellationToken.None));

    private static Playlist Playlist(params string[] titles) => new()
    {
        Title = "Stories",
        Chapters = titles.Select((title, i) => new Chapter
        {
            Key = i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Title = title,
            Tracks = [new Track { Key = "t" + i, Title = title, TrackUrl = "yoto:#x" }],
        }).ToList(),
    };

    private sealed class StubWriter(IReadOnlyList<string?> suggestions) : IAiTitleWriter
    {
        public string? PlaylistTitle { get; private set; }

        public IReadOnlyList<string> Titles { get; private set; } = [];

        public Task<IReadOnlyList<string?>> RewriteAsync(string playlistTitle, IReadOnlyList<string> trackTitles, CancellationToken cancellationToken)
        {
            (PlaylistTitle, Titles) = (playlistTitle, trackTitles);
            return Task.FromResult(suggestions);
        }
    }
}
