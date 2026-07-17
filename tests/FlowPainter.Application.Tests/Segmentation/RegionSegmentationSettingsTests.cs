using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionSegmentationSettingsTests
{
    [Fact]
    public void ConstructorUsesDocumentedDefaults()
    {
        RegionSegmentationSettings settings = new();

        Assert.True(settings.Enabled);
        Assert.Equal(64, settings.TargetRegionSize);
        Assert.Equal(10d, settings.Compactness);
        Assert.Equal(0.8d, settings.PreBlurSigma);
        Assert.Equal(10, settings.MaximumIterations);
        Assert.Equal(0.5d, settings.ConvergenceTolerance);
    }

    [Fact]
    public void ConstructorCanDisableSegmentationWithoutChangingParameters()
    {
        RegionSegmentationSettings settings = new(enabled: false);

        Assert.False(settings.Enabled);
        Assert.Equal(RegionSegmentationSettings.DefaultTargetRegionSize, settings.TargetRegionSize);
    }

    [Fact]
    public void ConstructorAcceptsBoundaryValues()
    {
        RegionSegmentationSettings settings = new(
            RegionSegmentationSettings.MinimumTargetRegionSize,
            0.01d,
            0d,
            1,
            0.01d);

        Assert.Equal(4, settings.TargetRegionSize);
        Assert.Equal(0d, settings.PreBlurSigma);
    }

    [Fact]
    public void ConstructorRejectsTargetRegionSizeOutsideRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(targetRegionSize: 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(targetRegionSize: 2_049));
    }

    [Fact]
    public void ConstructorRejectsInvalidCompactness()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(compactness: 0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(compactness: double.NaN));
    }

    [Fact]
    public void ConstructorRejectsInvalidPreBlurSigma()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(preBlurSigma: -0.1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(preBlurSigma: double.PositiveInfinity));
    }

    [Fact]
    public void ConstructorRejectsInvalidIterationCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(maximumIterations: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(maximumIterations: 101));
    }

    [Fact]
    public void ConstructorRejectsInvalidConvergenceTolerance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(convergenceTolerance: 0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationSettings(convergenceTolerance: double.NaN));
    }
}
