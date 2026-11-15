using OpenMyo.Core.Library;

namespace OpenMyo.Core.Tests.Library;

public class LibraryFilterTests
{
    [Theory]
    [InlineData(PlaylistIssues.None, PlaylistIssues.None, true)]
    [InlineData(PlaylistIssues.None, PlaylistIssues.NoTracks, true)]
    [InlineData(PlaylistIssues.NoTracks, PlaylistIssues.NoTracks, true)]
    [InlineData(PlaylistIssues.NoTracks, PlaylistIssues.MissingIcons | PlaylistIssues.MissingCover, false)]
    [InlineData(PlaylistIssues.MissingIcons, PlaylistIssues.MissingIcons | PlaylistIssues.NoTracks, true)]
    [InlineData(PlaylistIssues.MissingIcons, PlaylistIssues.None, false)]
    [InlineData(PlaylistIssues.MissingCover, PlaylistIssues.MissingCover, true)]
    [InlineData(PlaylistIssues.MissingCover, PlaylistIssues.MissingIcons, false)]
    [InlineData(PlaylistIssues.MissingIcons | PlaylistIssues.MissingCover, PlaylistIssues.MissingIcons | PlaylistIssues.MissingCover, true)]
    [InlineData(PlaylistIssues.MissingIcons | PlaylistIssues.MissingCover, PlaylistIssues.MissingIcons, false)]
    public void Matches_RequiredAndIssues_ReturnsExpected(PlaylistIssues required, PlaylistIssues issues, bool expected)
    {
        Assert.Equal(expected, LibraryFilter.Matches(required, issues));
    }
}
