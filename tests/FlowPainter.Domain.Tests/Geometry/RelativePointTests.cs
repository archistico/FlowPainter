using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Tests.Geometry;

public sealed class RelativePointTests
{
    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.PositiveInfinity)]
    public void ConstructorRejectsNonFiniteCoordinates(double x, double y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RelativePoint(x, y));
    }

    [Theory]
    [InlineData(0d, 0d, true)]
    [InlineData(1d, 1d, true)]
    [InlineData(-0.01d, 0.5d, false)]
    [InlineData(0.5d, 1.01d, false)]
    public void IsInsideCanvasReportsUnitSquareMembership(double x, double y, bool expected)
    {
        RelativePoint point = new(x, y);

        Assert.Equal(expected, point.IsInsideCanvas);
    }
}
