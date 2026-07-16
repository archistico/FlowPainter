using FlowPainter.Application.Boundaries;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class BoundaryPaintingSettingsTests
{
    [Fact]
    public void ConstructorUsesBackwardCompatibleDisabledDefault()
    {
        BoundaryPaintingSettings settings = new();

        Assert.False(settings.Enabled);
        Assert.Equal(BoundaryPaintingSettings.DefaultTangentAlignment, settings.TangentAlignment);
        Assert.Equal(BoundaryPaintingSettings.DefaultAlignmentRadius, settings.AlignmentRadius);
        Assert.Equal(BoundaryPaintingSettings.DefaultCrossingPenalty, settings.CrossingPenalty);
    }

    [Fact]
    public void ConstructorAcceptsMaximumSupportedValues()
    {
        BoundaryPaintingSettings settings = new(
            enabled: true,
            tangentAlignment: 1d,
            alignmentRadius: BoundaryPaintingSettings.MaximumAlignmentRadius,
            crossingPenalty: 1d,
            hardBoundaryThreshold: 1d,
            terminationStrength: 1d,
            internalEdgeInfluence: 1d,
            textureEdgeInfluence: 1d,
            contourReinforcement: 4d,
            cornerPreservation: 1d);

        Assert.True(settings.Enabled);
        Assert.Equal(4d, settings.ContourReinforcement);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidTangentAlignment(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoundaryPaintingSettings(tangentAlignment: value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(BoundaryPaintingSettings.MaximumAlignmentRadius + 1)]
    public void ConstructorRejectsInvalidAlignmentRadius(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoundaryPaintingSettings(alignmentRadius: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    public void ConstructorRejectsInvalidCrossingPenalty(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoundaryPaintingSettings(crossingPenalty: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(4.01d)]
    public void ConstructorRejectsInvalidContourReinforcement(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoundaryPaintingSettings(contourReinforcement: value));
    }
}
