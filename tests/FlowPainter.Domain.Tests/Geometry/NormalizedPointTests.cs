using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Tests.Geometry;

public sealed class NormalizedPointTests
{
    [Theory]
    [InlineData(-0.01, 0.5)]
    [InlineData(1.01, 0.5)]
    [InlineData(0.5, -0.01)]
    [InlineData(0.5, 1.01)]
    public void ConstructorRejectsCoordinatesOutsideUnitSquare(double x, double y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedPoint(x, y));
    }

    [Fact]
    public void ConstructorAcceptsBoundaryCoordinates()
    {
        Assert.Equal(new NormalizedPoint(0d, 0d), new NormalizedPoint(0d, 0d));
        Assert.Equal(new NormalizedPoint(1d, 1d), new NormalizedPoint(1d, 1d));
    }
}
