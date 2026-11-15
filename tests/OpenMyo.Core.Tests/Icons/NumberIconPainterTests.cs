using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Tests.Icons;

public sealed class NumberIconPainterTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(151)]
    public void Paint_ValidNumber_ReturnsPngSignature(int number)
    {
        var png = NumberIconPainter.Paint(number);

        Assert.Equal([0x89, 0x50, 0x4E, 0x47], png[..4]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void Paint_OutOfRange_Throws(int number) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberIconPainter.Paint(number));
}
