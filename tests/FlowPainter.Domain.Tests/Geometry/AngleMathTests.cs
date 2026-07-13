using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Tests.Geometry;

public sealed class AngleMathTests
{
    [Fact]
    public void ShortestDistanceRadiansUsesCircularDistanceAcrossTauBoundary()
    {
        double first = 0.05d;
        double second = AngleMath.Tau - 0.05d;

        double distance = AngleMath.ShortestDistanceRadians(first, second);

        Assert.Equal(0.1d, distance, 12);
    }

    [Theory]
    [InlineData(-1.5707963267948966, 4.71238898038469)]
    [InlineData(7.853981633974483, 1.5707963267948966)]
    public void NormalizeRadiansReturnsEquivalentAngleInCanonicalRange(double input, double expected)
    {
        Assert.Equal(expected, AngleMath.NormalizeRadians(input), 12);
    }

    [Fact]
    public void NormalizeRadiansRejectsNonFiniteAngle()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AngleMath.NormalizeRadians(double.PositiveInfinity));
    }
}
