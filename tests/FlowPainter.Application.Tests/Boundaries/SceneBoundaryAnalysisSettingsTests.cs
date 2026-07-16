using FlowPainter.Application.Boundaries;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class SceneBoundaryAnalysisSettingsTests
{
    [Fact]
    public void ConstructorUsesStableDefaults()
    {
        SceneBoundaryAnalysisSettings settings = new();

        Assert.True(settings.Enabled);
        Assert.Equal(SceneBoundaryAnalysisSettings.DefaultLuminanceWeight, settings.LuminanceWeight);
        Assert.Equal(SceneBoundaryAnalysisSettings.DefaultColorWeight, settings.ColorWeight);
        Assert.Equal(SceneBoundaryAnalysisSettings.DefaultCoarseRadius, settings.CoarseRadius);
        Assert.Equal(SceneBoundaryAnalysisSettings.DefaultBoundaryProtectionRadius, settings.BoundaryProtectionRadius);
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(4.1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidWeights(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneBoundaryAnalysisSettings(
            luminanceWeight: value));
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidUnitIntervalValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneBoundaryAnalysisSettings(
            textureSuppression: value));
    }

    [Fact]
    public void ConstructorRejectsImportantThresholdBelowGeneralThreshold()
    {
        Assert.Throws<ArgumentException>(() => new SceneBoundaryAnalysisSettings(
            edgeThreshold: 0.6d,
            importantEdgeThreshold: 0.4d));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SceneBoundaryAnalysisSettings.MaximumRadius + 1)]
    public void ConstructorRejectsInvalidCoarseRadius(int radius)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneBoundaryAnalysisSettings(
            coarseRadius: radius));
    }

    [Fact]
    public void ConstructorAllowsDisabledZeroSignalWeights()
    {
        SceneBoundaryAnalysisSettings settings = new(
            enabled: false,
            luminanceWeight: 0d,
            colorWeight: 0d);

        Assert.False(settings.Enabled);
    }

    [Fact]
    public void ConstructorRejectsEnabledZeroSignalWeights()
    {
        Assert.Throws<ArgumentException>(() => new SceneBoundaryAnalysisSettings(
            enabled: true,
            luminanceWeight: 0d,
            colorWeight: 0d));
    }
}
