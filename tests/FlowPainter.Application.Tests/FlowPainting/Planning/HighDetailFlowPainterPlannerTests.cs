using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class HighDetailFlowPainterPlannerTests
{
    [Fact]
    public void DetailedRegionUsesMoreShorterSegmentsThanBackground()
    {
        RgbaImage image = new(new ImageSize(1, 1), [Rgba32.Opaque(100, 100, 100)]);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        DetailInfluenceSettings influence = new(
            placementBias: 0d,
            detailedLengthMultiplier: 0.5d,
            backgroundLengthMultiplier: 1.5d,
            detailedWidthMultiplier: 1d,
            backgroundWidthMultiplier: 1d,
            detailedSegmentMultiplier: 2d,
            backgroundSegmentMultiplier: 0.5d,
            detailedCurveMultiplier: 1d,
            backgroundCurveMultiplier: 1d,
            detailedTangentAlignmentBoost: 0d,
            detailedCrossingResistanceBoost: 0d);
        FlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 4,
            lengthScale: 0.01d,
            minimumStrokeWidthPixels: 1d,
            maximumStrokeWidthPixels: 1d,
            detailInfluence: influence);
        FlowPainterPlanner planner = new(new ConstantFieldFactory());

        StrokePlan background = planner.CreatePlan(
            image,
            density,
            DetailMap.CreateUniform(image.Size, 0f),
            7UL,
            settings);
        StrokePlan detailed = planner.CreatePlan(
            image,
            density,
            DetailMap.CreateUniform(image.Size, 1f),
            7UL,
            settings);

        Assert.Equal(3, background.Strokes[0].Points.Count);
        Assert.Equal(9, detailed.Strokes[0].Points.Count);
        double backgroundStep = GetFirstStepLength(background.Strokes[0]);
        double detailedStep = GetFirstStepLength(detailed.Strokes[0]);
        Assert.True(detailedStep < backgroundStep);
    }

    [Fact]
    public void DetailPlanPublishesVersionTwoPolicyIdentity()
    {
        RgbaImage image = new(new ImageSize(1, 1), [Rgba32.Opaque(100, 100, 100)]);
        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory()).CreatePlan(
            image,
            StrokeDensityMap.CreateUniform(image.Size, 1d),
            DetailMap.CreateUniform(image.Size, 0.5f),
            1UL,
            new FlowPainterSettings(strokeCount: 1, segmentCount: 2));

        Assert.Equal("flow-field-detail-v2", plan.PlannerVersion);
    }

    private static double GetFirstStepLength(FlowStroke stroke)
    {
        double deltaX = stroke.Points[1].X - stroke.Points[0].X;
        double deltaY = stroke.Points[1].Y - stroke.Points[0].Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private sealed class ConstantFieldFactory : IFlowFieldFactory
    {
        public IFlowField Create(int seed, FlowFieldSettings settings)
        {
            return new CenterSeekingField();
        }
    }

    private sealed class CenterSeekingField : IFlowField
    {
        public double SampleAngle(double normalizedX, double normalizedY)
        {
            return normalizedX < 0.5d ? 0d : Math.PI;
        }
    }
}
