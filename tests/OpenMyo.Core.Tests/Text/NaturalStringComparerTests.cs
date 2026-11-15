using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public class NaturalStringComparerTests
{
    private static readonly NaturalStringComparer Comparer = NaturalStringComparer.Instance;

    [Fact]
    public void Compare_NumberRuns_ComparesNumerically()
    {
        Assert.True(Comparer.Compare("Track 2", "Track 10") < 0);
        Assert.True(Comparer.Compare("Track 10", "Track 2") > 0);
    }

    [Fact]
    public void Compare_LeadingZeros_TreatsEqualNumbersAsEqual()
    {
        Assert.Equal(0, Comparer.Compare("Track 02", "Track 2"));
    }

    [Fact]
    public void Compare_LeadingZerosOnLargerNumber_StillOrdersNumerically()
    {
        Assert.True(Comparer.Compare("Track 002", "Track 10") < 0);
    }

    [Fact]
    public void Compare_DifferentCase_IsCaseInsensitive()
    {
        Assert.Equal(0, Comparer.Compare("abc", "ABC"));
    }

    [Fact]
    public void Compare_BothNull_ReturnsZero()
    {
        Assert.Equal(0, Comparer.Compare(null, null));
    }

    [Fact]
    public void Compare_NullAgainstText_NullSortsFirst()
    {
        Assert.True(Comparer.Compare(null, "a") < 0);
        Assert.True(Comparer.Compare("a", null) > 0);
    }

    [Fact]
    public void Compare_PrefixAgainstLongerString_ShorterSortsFirst()
    {
        Assert.True(Comparer.Compare("Track", "Track 1") < 0);
    }

    [Fact]
    public void Sort_MixedNames_ProducesNaturalOrder()
    {
        var names = new List<string> { "b10", "b2", "a1", "B1" };

        names.Sort(Comparer);

        Assert.Equal(["a1", "B1", "b2", "b10"], names);
    }
}
