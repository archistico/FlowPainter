using FlowPainter.Application.FlowPainting.Legacy;

namespace FlowPainter.Application.Tests.FlowPainting.Legacy;

public sealed class LegacyFlowPainterSettingsTests
{
    [Fact]
    public void DefaultsMatchOriginalFlowPainterConstants()
    {
        LegacyFlowPainterSettings settings = new();

        Assert.Equal(50_000, settings.StrokeCount);
        Assert.Equal(20, settings.SegmentCount);
        Assert.Equal(512, settings.ReferenceMaximumDimension);
        Assert.Equal(1d, settings.NoiseScale);
        Assert.Equal(0.005d, settings.LengthScale);
        Assert.Equal(0.5d, settings.MaximumCurveRadians);
        Assert.Equal(5d, settings.MinimumStrokeWidthPixels);
        Assert.Equal(10d, settings.MaximumStrokeWidthPixels);
    }

    [Fact]
    public void ConstructorRejectsMaximumWidthBelowMinimum()
    {
        Assert.Throws<ArgumentException>(
            () => new LegacyFlowPainterSettings(
                minimumStrokeWidthPixels: 10d,
                maximumStrokeWidthPixels: 5d));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsInvalidStrokeCount(int strokeCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LegacyFlowPainterSettings(strokeCount: strokeCount));
    }

    [Fact]
    public void ConstructorRejectsReferenceDimensionAboveImageLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LegacyFlowPainterSettings(referenceMaximumDimension: 10_001));
    }
}
