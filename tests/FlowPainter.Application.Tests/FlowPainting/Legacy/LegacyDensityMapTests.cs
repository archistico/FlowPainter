using FlowPainter.Application.FlowPainting.Legacy;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.FlowPainting.Legacy;

public sealed class LegacyDensityMapTests
{
    [Fact]
    public void ConstructorCopiesValues()
    {
        double[] values = [1d];
        LegacyDensityMap map = new(new ImageSize(1, 1), values);

        values[0] = 9d;

        Assert.Equal(1d, map[0, 0]);
    }

    [Fact]
    public void ConstructorRejectsIncorrectValueCount()
    {
        Assert.Throws<ArgumentException>(
            () => new LegacyDensityMap(new ImageSize(2, 2), [1d]));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidDensity(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LegacyDensityMap(new ImageSize(1, 1), [value]));
    }

    [Fact]
    public void SampleNearestUsesRowMajorCoordinates()
    {
        LegacyDensityMap map = new(new ImageSize(2, 2), [1d, 2d, 3d, 4d]);

        Assert.Equal(1d, map.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(4d, map.SampleNearest(new NormalizedPoint(1d, 1d)));
    }
}
