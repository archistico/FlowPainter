using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class FlowPainterSettingsTests
{
    [Fact]
    public void ConstructorUsesBalancedDefaults()
    {
        FlowPainterSettings settings = new();

        Assert.Equal(FlowPainterSettings.DefaultStrokeCount, settings.StrokeCount);
        Assert.Equal(FlowPainterSettings.DefaultSegmentCount, settings.SegmentCount);
        Assert.Equal(FlowPainterSettings.DefaultUniformDensity, settings.UniformDensity);
        Assert.Equal(FlowPainterSettings.DefaultStrokeOpacity, settings.StrokeOpacity);
        Assert.Equal(StrokePlanBackgroundMode.SourceImage, settings.BackgroundMode);
        Assert.NotNull(settings.Field);
        Assert.NotNull(settings.DetailAnalysis);
        Assert.NotNull(settings.DetailInfluence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_001)]
    public void ConstructorRejectsUnsupportedStrokeCount(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowPainterSettings(strokeCount: value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_025)]
    public void ConstructorRejectsUnsupportedSegmentCount(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowPainterSettings(segmentCount: value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidDensity(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowPainterSettings(uniformDensity: value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidLengthScale(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowPainterSettings(lengthScale: value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(6.283185307179587d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidMaximumCurve(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowPainterSettings(maximumCurveRadians: value));
    }

    [Fact]
    public void ConstructorRejectsReversedStrokeWidths()
    {
        Assert.Throws<ArgumentException>(
            () => new FlowPainterSettings(
                minimumStrokeWidthPixels: 5d,
                maximumStrokeWidthPixels: 4d));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidOpacity(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowPainterSettings(strokeOpacity: value));
    }

    [Fact]
    public void ConstructorRetainsProvidedFieldSettings()
    {
        FlowFieldSettings field = new(scale: 8d);

        FlowPainterSettings settings = new(field);

        Assert.Same(field, settings.Field);
    }

    [Fact]
    public void ConstructorRetainsProvidedDetailSettings()
    {
        DetailAnalysisSettings analysis = new(baseDetail: 0.3d);
        DetailInfluenceSettings influence = new(placementBias: 7d);

        FlowPainterSettings settings = new(
            detailAnalysis: analysis,
            detailInfluence: influence);

        Assert.Same(analysis, settings.DetailAnalysis);
        Assert.Same(influence, settings.DetailInfluence);
    }
}
