using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Boundaries;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class HighDetailStrokePolicyTests
{
    [Fact]
    public void GeometryUsesConfiguredBackgroundAndDetailEndpoints()
    {
        DetailInfluenceSettings settings = new(
            detailedLengthMultiplier: 0.4d,
            backgroundLengthMultiplier: 1.6d,
            detailedWidthMultiplier: 0.5d,
            backgroundWidthMultiplier: 1.5d,
            detailedSegmentMultiplier: 2d,
            backgroundSegmentMultiplier: 0.5d,
            detailedCurveMultiplier: 1.5d,
            backgroundCurveMultiplier: 0.75d);

        LocalStrokeGeometry background = HighDetailStrokePolicy.EvaluateGeometry(0d, settings);
        LocalStrokeGeometry detail = HighDetailStrokePolicy.EvaluateGeometry(1d, settings);

        Assert.Equal(new LocalStrokeGeometry(1.6d, 1.5d, 0.5d, 0.75d), background);
        Assert.Equal(new LocalStrokeGeometry(0.4d, 0.5d, 2d, 1.5d), detail);
    }

    [Fact]
    public void GeometryInterpolatesContinuouslyAtMidDetail()
    {
        DetailInfluenceSettings settings = new(
            detailedLengthMultiplier: 0.5d,
            backgroundLengthMultiplier: 1.5d,
            detailedWidthMultiplier: 0.5d,
            backgroundWidthMultiplier: 1.5d,
            detailedSegmentMultiplier: 1.5d,
            backgroundSegmentMultiplier: 0.5d,
            detailedCurveMultiplier: 1.25d,
            backgroundCurveMultiplier: 0.75d);

        LocalStrokeGeometry sample = HighDetailStrokePolicy.EvaluateGeometry(0.5d, settings);

        Assert.Equal(new LocalStrokeGeometry(1d, 1d, 1d, 1d), sample);
    }

    [Fact]
    public void TangentAlignmentPreservesBasePolicyAtZeroDetail()
    {
        BoundaryGuidanceSample guidance = CreateGuidance(influence: 0.6d, regionalStrength: 0.8d);
        BoundaryPaintingSettings boundary = new(tangentAlignment: 0.5d);

        double amount = HighDetailStrokePolicy.GetTangentAlignmentAmount(
            0d,
            guidance,
            new DetailInfluenceSettings(detailedTangentAlignmentBoost: 0.8d),
            boundary);

        Assert.Equal(0.3d, amount, 12);
    }

    [Fact]
    public void DetailedBoundaryProgressivelyStrengthensTangentAlignment()
    {
        BoundaryGuidanceSample guidance = CreateGuidance(influence: 0.5d, regionalStrength: 0.75d);
        DetailInfluenceSettings detail = new(detailedTangentAlignmentBoost: 0.4d);
        BoundaryPaintingSettings boundary = new(tangentAlignment: 0.5d);

        double background = HighDetailStrokePolicy.GetTangentAlignmentAmount(0d, guidance, detail, boundary);
        double middle = HighDetailStrokePolicy.GetTangentAlignmentAmount(0.5d, guidance, detail, boundary);
        double detailed = HighDetailStrokePolicy.GetTangentAlignmentAmount(1d, guidance, detail, boundary);

        Assert.True(background < middle);
        Assert.True(middle < detailed);
        Assert.Equal(0.55d, detailed, 12);
    }

    [Fact]
    public void UndefinedBoundaryHasNoTangentAlignment()
    {
        BoundaryGuidanceSample guidance = new(
            1d,
            1d,
            1d,
            0d,
            default,
            1d,
            0d,
            default,
            true);

        double amount = HighDetailStrokePolicy.GetTangentAlignmentAmount(
            1d,
            guidance,
            new DetailInfluenceSettings(),
            new BoundaryPaintingSettings());

        Assert.Equal(0d, amount);
    }

    [Fact]
    public void CrossingResistancePreservesBaseAtZeroDetail()
    {
        BoundaryPaintingSettings boundary = new(crossingPenalty: 0.45d);

        double resistance = HighDetailStrokePolicy.GetCrossingResistance(
            0d,
            CreateGuidance(0.8d, 0.9d),
            new DetailInfluenceSettings(detailedCrossingResistanceBoost: 0.5d),
            boundary);

        Assert.Equal(0.45d, resistance, 12);
    }

    [Fact]
    public void DetailedBoundaryIncreasesCrossingResistance()
    {
        BoundaryPaintingSettings boundary = new(crossingPenalty: 0.4d);
        DetailInfluenceSettings detail = new(detailedCrossingResistanceBoost: 0.4d);
        BoundaryGuidanceSample guidance = CreateGuidance(0.5d, 0.75d);

        double background = HighDetailStrokePolicy.GetCrossingResistance(0d, guidance, detail, boundary);
        double detailed = HighDetailStrokePolicy.GetCrossingResistance(1d, guidance, detail, boundary);

        Assert.Equal(0.4d, background, 12);
        Assert.Equal(0.7d, detailed, 12);
    }

    [Fact]
    public void BoundaryResponsesClampToUnitInterval()
    {
        BoundaryGuidanceSample guidance = CreateGuidance(1d, 1d);
        DetailInfluenceSettings detail = new(
            detailedTangentAlignmentBoost: 1d,
            detailedCrossingResistanceBoost: 1d);
        BoundaryPaintingSettings boundary = new(
            tangentAlignment: 1d,
            crossingPenalty: 1d);

        Assert.Equal(1d, HighDetailStrokePolicy.GetTangentAlignmentAmount(1d, guidance, detail, boundary));
        Assert.Equal(1d, HighDetailStrokePolicy.GetCrossingResistance(1d, guidance, detail, boundary));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void PolicyRejectsInvalidDetail(double value)
    {
        DetailInfluenceSettings detail = new();
        BoundaryGuidanceSample guidance = CreateGuidance(1d, 1d);
        BoundaryPaintingSettings boundary = new();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => HighDetailStrokePolicy.EvaluateGeometry(value, detail));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HighDetailStrokePolicy.GetTangentAlignmentAmount(value, guidance, detail, boundary));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HighDetailStrokePolicy.GetCrossingResistance(value, guidance, detail, boundary));
    }

    private static BoundaryGuidanceSample CreateGuidance(double influence, double regionalStrength)
    {
        return new BoundaryGuidanceSample(
            influence,
            influence,
            0d,
            0d,
            new BoundaryVector(1d, 0d),
            regionalStrength,
            0d,
            new BoundaryVector(0d, 1d),
            false);
    }
}
