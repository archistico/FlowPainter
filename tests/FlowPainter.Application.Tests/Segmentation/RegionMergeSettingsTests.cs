using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionMergeSettingsTests
{
    [Fact]
    public void DefaultsDescribeThreeScaleHierarchy()
    {
        RegionMergeSettings settings = new();

        Assert.Equal(0.60d, settings.IntermediateTargetRatio);
        Assert.Equal(0.30d, settings.BroadMassTargetRatio);
        Assert.True(settings.BroadMassMaximumCost >= settings.IntermediateMaximumCost);
        Assert.True(settings.StrongBoundaryThreshold > settings.IntermediateMaximumCost);
    }

    [Fact]
    public void ConstructorPreservesCustomValues()
    {
        RegionMergeSettings settings = new(0.7d, 0.4d, 0.3d, 0.8d, 0.9d, 0.6d);

        Assert.Equal(0.7d, settings.IntermediateTargetRatio);
        Assert.Equal(0.4d, settings.BroadMassTargetRatio);
        Assert.Equal(0.3d, settings.IntermediateMaximumCost);
        Assert.Equal(0.8d, settings.BroadMassMaximumCost);
        Assert.Equal(0.9d, settings.StrongBoundaryThreshold);
        Assert.Equal(0.6d, settings.MaximumParentAreaFraction);
    }

    [Fact]
    public void ConstructorRejectsInvalidRatios()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionMergeSettings(
            intermediateTargetRatio: 0d));
        Assert.Throws<ArgumentException>(() => new RegionMergeSettings(
            intermediateTargetRatio: 0.4d,
            broadMassTargetRatio: 0.5d));
    }

    [Fact]
    public void ConstructorRejectsDescendingCosts()
    {
        Assert.Throws<ArgumentException>(() => new RegionMergeSettings(
            intermediateMaximumCost: 0.8d,
            broadMassMaximumCost: 0.7d));
    }

    [Fact]
    public void ConstructorRejectsInvalidBoundaryAndAreaLimits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionMergeSettings(
            strongBoundaryThreshold: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionMergeSettings(
            maximumParentAreaFraction: 0d));
    }
}
