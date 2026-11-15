using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class SeriesWordsTests
{
    [Fact]
    public void Find_WordInMostTitles_IsSeriesWord()
    {
        var words = SeriesWords.Find(["Odyssey: Owls", "Odyssey: Trains", "Odyssey: Castles", "Bonus Fox"]);

        Assert.Equal(["odyssey"], words);
    }

    [Fact]
    public void Find_FewTitles_FindsNone() =>
        Assert.Empty(SeriesWords.Find(["Owl story", "Owl song"]));
}
