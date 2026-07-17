using FlowPainter.Application.FlowPainting.Planning;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class DetailInfluenceSettingsTests
{
    [Fact]
    public void ConstructorUsesPainterlyDefaults()
    {
        DetailInfluenceSettings settings = new();

        Assert.Equal(DetailInfluenceSettings.DefaultPlacementBias, settings.PlacementBias);
        Assert.Equal(DetailInfluenceSettings.DefaultDetailedLengthMultiplier, settings.DetailedLengthMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultBackgroundLengthMultiplier, settings.BackgroundLengthMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultDetailedWidthMultiplier, settings.DetailedWidthMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultBackgroundWidthMultiplier, settings.BackgroundWidthMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultRegionTransitionWidth, settings.RegionTransitionWidth);
        Assert.Equal(DetailInfluenceSettings.DefaultDetailedSegmentMultiplier, settings.DetailedSegmentMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultBackgroundSegmentMultiplier, settings.BackgroundSegmentMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultDetailedCurveMultiplier, settings.DetailedCurveMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultBackgroundCurveMultiplier, settings.BackgroundCurveMultiplier);
        Assert.Equal(DetailInfluenceSettings.DefaultDetailedTangentAlignmentBoost, settings.DetailedTangentAlignmentBoost);
        Assert.Equal(DetailInfluenceSettings.DefaultDetailedCrossingResistanceBoost, settings.DetailedCrossingResistanceBoost);
    }

    [Fact]
    public void PlacementWeightIncreasesWithDetail()
    {
        DetailInfluenceSettings settings = new(placementBias: 5d);

        Assert.Equal(1d, settings.GetPlacementWeight(0d));
        Assert.Equal(6d, settings.GetPlacementWeight(1d));
    }

    [Fact]
    public void LengthMultiplierInterpolatesFromBackgroundToDetail()
    {
        DetailInfluenceSettings settings = new(
            detailedLengthMultiplier: 0.5d,
            backgroundLengthMultiplier: 1.5d);

        Assert.Equal(1.5d, settings.GetLengthMultiplier(0d));
        Assert.Equal(1d, settings.GetLengthMultiplier(0.5d));
        Assert.Equal(0.5d, settings.GetLengthMultiplier(1d));
    }

    [Fact]
    public void WidthMultiplierInterpolatesFromBackgroundToDetail()
    {
        DetailInfluenceSettings settings = new(
            detailedWidthMultiplier: 0.75d,
            backgroundWidthMultiplier: 1.25d);

        Assert.Equal(1.25d, settings.GetWidthMultiplier(0d));
        Assert.Equal(0.75d, settings.GetWidthMultiplier(1d));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(20.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidPlacementBias(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailInfluenceSettings(placementBias: value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(4.01d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidMultipliers(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailInfluenceSettings(detailedLengthMultiplier: value));
    }



    [Fact]
    public void SegmentMultiplierInterpolatesContinuously()
    {
        DetailInfluenceSettings settings = new(
            detailedSegmentMultiplier: 2d,
            backgroundSegmentMultiplier: 0.5d);

        Assert.Equal(0.5d, settings.GetSegmentMultiplier(0d));
        Assert.Equal(1.25d, settings.GetSegmentMultiplier(0.5d));
        Assert.Equal(2d, settings.GetSegmentMultiplier(1d));
    }

    [Fact]
    public void CurveMultiplierInterpolatesContinuously()
    {
        DetailInfluenceSettings settings = new(
            detailedCurveMultiplier: 1.5d,
            backgroundCurveMultiplier: 0.75d);

        Assert.Equal(0.75d, settings.GetCurveMultiplier(0d));
        Assert.Equal(1.125d, settings.GetCurveMultiplier(0.5d));
        Assert.Equal(1.5d, settings.GetCurveMultiplier(1d));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidBoundaryBoosts(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailInfluenceSettings(detailedTangentAlignmentBoost: value));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailInfluenceSettings(detailedCrossingResistanceBoost: value));
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(0.501d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidRegionTransitionWidth(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailInfluenceSettings(regionTransitionWidth: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void SamplingMethodsRejectInvalidDetail(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailInfluenceSettings().GetPlacementWeight(value));
    }
}
