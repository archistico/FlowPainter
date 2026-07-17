using FlowPainter.Application.Boundaries;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class RegionalBoundaryFieldSettingsTests
{
    [Fact]
    public void DefaultsRepresentSoftRegionalGuidance()
    {
        RegionalBoundaryFieldSettings settings = new();

        Assert.Equal(10, settings.MaximumDistancePixels);
        Assert.Equal(0.62d, settings.HardBarrierThreshold, 12);
        Assert.Equal(0.45d, settings.HardTransitionRadiusFactor, 12);
        Assert.Equal(0.65d, settings.SoftTransitionExponent, 12);
    }

    [Fact]
    public void FromBoundaryPaintingUsesAlignmentAndBarrierThreshold()
    {
        BoundaryPaintingSettings boundary = new(
            enabled: true,
            alignmentRadius: 7,
            hardBoundaryThreshold: 0.71d);

        RegionalBoundaryFieldSettings settings = RegionalBoundaryFieldSettings.FromBoundaryPainting(boundary);

        Assert.Equal(14, settings.MaximumDistancePixels);
        Assert.Equal(0.71d, settings.HardBarrierThreshold, 12);
    }

    [Fact]
    public void ConstructorRejectsUnsupportedDistance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(65));
    }

    [Fact]
    public void ConstructorRejectsInvalidBarrierThreshold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            hardBarrierThreshold: -0.01d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            hardBarrierThreshold: 1.01d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            hardBarrierThreshold: double.NaN));
    }

    [Fact]
    public void ConstructorRejectsInvalidTransitionShape()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            hardTransitionRadiusFactor: 0.09d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            hardTransitionRadiusFactor: 1.01d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            softTransitionExponent: 0.19d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionalBoundaryFieldSettings(
            softTransitionExponent: 1.01d));
    }
}
