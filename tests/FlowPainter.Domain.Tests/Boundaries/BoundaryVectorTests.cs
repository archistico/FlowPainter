using FlowPainter.Domain.Boundaries;

namespace FlowPainter.Domain.Tests.Boundaries;

public sealed class BoundaryVectorTests
{
    [Fact]
    public void ConstructorNormalizesDefinedVector()
    {
        BoundaryVector vector = new(3d, 4d);

        Assert.Equal(0.6d, vector.X, 12);
        Assert.Equal(0.8d, vector.Y, 12);
        Assert.True(vector.IsDefined);
    }

    [Fact]
    public void ConstructorKeepsZeroVectorUndefined()
    {
        BoundaryVector vector = new(0d, 0d);

        Assert.Equal(0d, vector.X);
        Assert.Equal(0d, vector.Y);
        Assert.False(vector.IsDefined);
    }

    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(double.PositiveInfinity, 0d)]
    [InlineData(0d, double.NegativeInfinity)]
    public void ConstructorRejectsNonFiniteComponents(double x, double y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundaryVector(x, y));
    }

    [Fact]
    public void DotReturnsDirectionalAlignment()
    {
        BoundaryVector horizontal = new(1d, 0d);
        BoundaryVector vertical = new(0d, 1d);
        BoundaryVector reverse = new(-1d, 0d);

        Assert.Equal(0d, horizontal.Dot(vertical), 12);
        Assert.Equal(-1d, horizontal.Dot(reverse), 12);
    }
}
