using FlowPainter.Application.Background;

namespace FlowPainter.Application.Tests.Background;

public sealed class BackgroundSuppressionSettingsTests
{
    [Fact]
    public void DefaultsPreserveLegacyBehavior()
    {
        BackgroundSuppressionSettings settings = new();

        Assert.False(settings.Enabled);
        Assert.Equal(BackgroundSuppressionSettings.DefaultDetailFloor, settings.DetailFloor);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidOverallStrength(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BackgroundSuppressionSettings(overallStrength: value));
    }

    [Theory]
    [InlineData(0d, 1d)]
    [InlineData(0.5d, 0.66d)]
    [InlineData(1d, 0.32d)]
    public void PlacementMultiplierInterpolatesTowardBackgroundWeight(double suppression, double expected)
    {
        BackgroundSuppressionSettings settings = new(backgroundPlacementWeight: 0.32d);

        Assert.Equal(expected, settings.GetPlacementMultiplier(suppression), 12);
    }

    [Fact]
    public void FullSuppressionUsesConfiguredPainterlyMultipliers()
    {
        BackgroundSuppressionSettings settings = new(
            strokeLengthMultiplier: 2.1d,
            strokeWidthMultiplier: 1.8d,
            segmentMultiplier: 0.45d,
            curveFreedomMultiplier: 1.9d);

        Assert.Equal(2.1d, settings.GetStrokeLengthMultiplier(1d), 12);
        Assert.Equal(1.8d, settings.GetStrokeWidthMultiplier(1d), 12);
        Assert.Equal(0.45d, settings.GetSegmentMultiplier(1d), 12);
        Assert.Equal(1.9d, settings.GetCurveFreedomMultiplier(1d), 12);
    }
}
