using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionSizeDistributionTests
{
    private static readonly int[] ExampleSizes = [2, 4, 6];
    private static readonly int[] SingleRegionSize = [7];
    private static readonly int[] EmptySizes = [];

    [Fact]
    public void CreateCalculatesDistribution()
    {
        RegionSizeDistribution distribution = RegionSizeDistribution.Create(ExampleSizes);

        Assert.Equal(2, distribution.MinimumPixelCount);
        Assert.Equal(6, distribution.MaximumPixelCount);
        Assert.Equal(4d, distribution.MeanPixelCount);
        Assert.Equal(Math.Sqrt(8d / 3d), distribution.StandardDeviationPixelCount, 12);
    }

    [Fact]
    public void CreateAcceptsSingleRegion()
    {
        RegionSizeDistribution distribution = RegionSizeDistribution.Create(SingleRegionSize);

        Assert.Equal(7d, distribution.MeanPixelCount);
        Assert.Equal(0d, distribution.StandardDeviationPixelCount);
    }

    [Fact]
    public void CreateRejectsEmptyInput()
    {
        Assert.Throws<ArgumentException>(() => RegionSizeDistribution.Create(EmptySizes));
    }

    [Fact]
    public void ConstructorRejectsInconsistentRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSizeDistribution(5, 4, 4.5d, 0d));
    }
}
