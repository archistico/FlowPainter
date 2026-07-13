using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class StrokeDensityMapTests
{
    [Fact]
    public void CreateUniformFillsEveryCell()
    {
        StrokeDensityMap map = StrokeDensityMap.CreateUniform(new ImageSize(3, 2), 7.5d);

        Assert.All(map.CopyValues(), value => Assert.Equal(7.5d, value));
    }

    [Fact]
    public void ConstructorCopiesInputValues()
    {
        double[] values = [1d, 2d];
        StrokeDensityMap map = new(new ImageSize(2, 1), values);

        values[0] = 9d;

        Assert.Equal(1d, map[0, 0]);
    }

    [Fact]
    public void SampleNearestMapsNormalizedCorners()
    {
        StrokeDensityMap map = new(new ImageSize(2, 2), [1d, 2d, 3d, 4d]);

        Assert.Equal(1d, map.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(4d, map.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public void ConstructorRejectsMismatchedValueCount()
    {
        Assert.Throws<ArgumentException>(
            () => new StrokeDensityMap(new ImageSize(2, 2), [1d]));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidDensity(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new StrokeDensityMap(new ImageSize(1, 1), [value]));
    }
}
