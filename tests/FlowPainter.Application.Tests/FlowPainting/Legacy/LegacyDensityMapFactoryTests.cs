using FlowPainter.Application.FlowPainting.Legacy;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.FlowPainting.Legacy;

public sealed class LegacyDensityMapFactoryTests
{
    [Fact]
    public void CreateUniformFillsEveryCell()
    {
        LegacyDensityMap map = LegacyDensityMapFactory.CreateUniform(new ImageSize(3, 2), 12.5d);

        Assert.All(map.CopyValues(), value => Assert.Equal(12.5d, value));
        Assert.Equal(12.5d, map.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CreateUniformRejectsInvalidDensity(double density)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LegacyDensityMapFactory.CreateUniform(new ImageSize(1, 1), density));
    }
}
