using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionMergeIntensityMapperTests
{
    [Fact]
    public void CreateDefaultMatchesDocumentedMergeDefaults()
    {
        RegionMergeSettings settings = RegionMergeIntensityMapper.Create(
            RegionMergeIntensityMapper.DefaultPercentage);

        Assert.Equal(RegionMergeSettings.DefaultIntermediateTargetRatio, settings.IntermediateTargetRatio);
        Assert.Equal(RegionMergeSettings.DefaultBroadMassTargetRatio, settings.BroadMassTargetRatio);
        Assert.Equal(RegionMergeSettings.DefaultIntermediateMaximumCost, settings.IntermediateMaximumCost);
        Assert.Equal(RegionMergeSettings.DefaultBroadMassMaximumCost, settings.BroadMassMaximumCost);
        Assert.Equal(RegionMergeSettings.DefaultStrongBoundaryThreshold, settings.StrongBoundaryThreshold);
        Assert.Equal(RegionMergeSettings.DefaultMaximumParentAreaFraction, settings.MaximumParentAreaFraction);
    }

    [Fact]
    public void CreateZeroProducesConservativeMergePolicy()
    {
        RegionMergeSettings settings = RegionMergeIntensityMapper.Create(0d);

        Assert.Equal(0.90d, settings.IntermediateTargetRatio);
        Assert.Equal(0.55d, settings.BroadMassTargetRatio);
        Assert.Equal(0.22d, settings.IntermediateMaximumCost);
        Assert.Equal(0.42d, settings.BroadMassMaximumCost);
        Assert.Equal(0.90d, settings.StrongBoundaryThreshold);
        Assert.Equal(0.25d, settings.MaximumParentAreaFraction);
    }

    [Fact]
    public void CreateHundredProducesAggressiveMergePolicy()
    {
        RegionMergeSettings settings = RegionMergeIntensityMapper.Create(100d);

        Assert.Equal(0.30d, settings.IntermediateTargetRatio, 12);
        Assert.Equal(0.05d, settings.BroadMassTargetRatio, 12);
        Assert.Equal(0.62d, settings.IntermediateMaximumCost, 12);
        Assert.Equal(0.82d, settings.BroadMassMaximumCost, 12);
        Assert.Equal(0.54d, settings.StrongBoundaryThreshold, 12);
        Assert.Equal(0.65d, settings.MaximumParentAreaFraction, 12);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(25d)]
    [InlineData(50d)]
    [InlineData(75d)]
    [InlineData(100d)]
    public void EstimatePercentageRoundTripsMappedSettings(double percentage)
    {
        RegionMergeSettings settings = RegionMergeIntensityMapper.Create(percentage);

        Assert.Equal(percentage, RegionMergeIntensityMapper.EstimatePercentage(settings), 10);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(100.01d)]
    public void CreateRejectsOutOfRangePercentage(double percentage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionMergeIntensityMapper.Create(percentage));
    }

    [Fact]
    public void CreateRejectsNonFinitePercentage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionMergeIntensityMapper.Create(double.NaN));
    }
}
