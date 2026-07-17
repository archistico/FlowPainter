using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class BackgroundAwareFlowPainterPlannerTests
{
    [Fact]
    public void DisabledSuppressionPreservesDetailPlanExactly()
    {
        RgbaImage image = CreateUniformImage(4, 4);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        BackgroundSuppressionResult suppression = BackgroundSuppressionResult.CreateDisabled(detail);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        FlowPainterSettings settings = CreateSettings(new BackgroundSuppressionSettings(enabled: false));
        FlowPainterPlanner planner = new(new ConstantFieldFactory(0d));

        StrokePlan expected = planner.CreatePlan(image, density, detail, 27UL, settings);
        StrokePlan actual = planner.CreatePlan(
            image,
            density,
            suppression,
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            27UL,
            settings);

        Assert.Equal(FlowPainterPlanner.DetailPlannerVersion, actual.PlannerVersion);
        AssertPlansEqual(expected, actual);
    }

    [Fact]
    public void SuppressionPlacesFewerStartsInBackgroundHalf()
    {
        RgbaImage image = new(
            new ImageSize(2, 1),
            [Rgba32.Opaque(20, 20, 20), Rgba32.Opaque(220, 220, 220)]);
        DetailMap effective = new(2, 1, [0.1f, 0.9f]);
        BackgroundSuppressionResult suppression = new(
            new ArtisticDetailField(2, 1, [-1f, 1f]),
            new DetailMap(2, 1, [1f, 0f]),
            new DetailMap(2, 1, [0f, 1f]),
            effective);
        FlowPainterSettings settings = CreateSettings(new BackgroundSuppressionSettings(
            enabled: true,
            backgroundPlacementWeight: 0.1d));

        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            StrokeDensityMap.CreateUniform(image.Size, 4d),
            suppression,
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            91UL,
            settings);

        int backgroundStarts = plan.Strokes.Count(stroke => stroke.Points[0].X < 0.5d);
        Assert.Equal(FlowPainterPlanner.BackgroundPlannerVersion, plan.PlannerVersion);
        Assert.InRange(backgroundStarts, 0, 250);
    }

    [Fact]
    public void SuppressedAreaUsesWiderLongerAndSimplerStroke()
    {
        RgbaImage image = CreateUniformImage(1, 1);
        DetailMap effective = DetailMap.CreateUniform(image.Size, 0.5f);
        FlowPainterSettings settings = CreateSettings(new BackgroundSuppressionSettings(
            enabled: true,
            strokeLengthMultiplier: 2d,
            strokeWidthMultiplier: 1.5d,
            segmentMultiplier: 0.5d,
            curveFreedomMultiplier: 1.5d));
        FlowPainterPlanner planner = new(new ConstantFieldFactory(0d));

        StrokePlan background = planner.CreatePlan(
            image,
            StrokeDensityMap.CreateUniform(image.Size, 4d),
            CreateSuppression(image.Size, -1f, 1f, effective),
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            14UL,
            settings);
        StrokePlan protectedPlan = planner.CreatePlan(
            image,
            StrokeDensityMap.CreateUniform(image.Size, 4d),
            CreateSuppression(image.Size, 1f, 0f, effective),
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            14UL,
            settings);

        Assert.True(background.Strokes[0].WidthRelativeToReference > protectedPlan.Strokes[0].WidthRelativeToReference);
        Assert.True(background.Strokes[0].Points.Count < protectedPlan.Strokes[0].Points.Count);
        Assert.True(PathLength(background.Strokes[0]) > PathLength(protectedPlan.Strokes[0]));
    }

    [Fact]
    public void BackgroundAwarePlanIsDeterministic()
    {
        RgbaImage image = CreateUniformImage(4, 4);
        DetailMap effective = DetailMap.CreateUniform(image.Size, 0.3f);
        BackgroundSuppressionResult suppression = CreateSuppression(image.Size, -0.7f, 0.7f, effective);
        FlowPainterSettings settings = CreateSettings(new BackgroundSuppressionSettings(enabled: true));
        FlowPainterPlanner planner = new(new ConstantFieldFactory(0.4d));

        StrokePlan first = planner.CreatePlan(
            image,
            StrokeDensityMap.CreateUniform(image.Size, 4d),
            suppression,
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            72UL,
            settings);
        StrokePlan second = planner.CreatePlan(
            image,
            StrokeDensityMap.CreateUniform(image.Size, 4d),
            suppression,
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            72UL,
            settings);

        AssertPlansEqual(first, second);
    }

    private static FlowPainterSettings CreateSettings(BackgroundSuppressionSettings backgroundSuppression)
    {
        return new FlowPainterSettings(
            strokeCount: 2_000,
            segmentCount: 10,
            uniformDensity: 4d,
            lengthScale: 0.0001d,
            maximumCurveRadians: AngleMath.Tau,
            minimumStrokeWidthPixels: 2d,
            maximumStrokeWidthPixels: 2d,
            detailInfluence: new DetailInfluenceSettings(
                placementBias: 0d,
                detailedLengthMultiplier: 1d,
                backgroundLengthMultiplier: 1d,
                detailedWidthMultiplier: 1d,
                backgroundWidthMultiplier: 1d,
                detailedSegmentMultiplier: 1d,
                backgroundSegmentMultiplier: 1d,
                detailedCurveMultiplier: 1d,
                backgroundCurveMultiplier: 1d,
                detailedTangentAlignmentBoost: 0d,
                detailedCrossingResistanceBoost: 0d),
            backgroundSuppression: backgroundSuppression);
    }

    private static RgbaImage CreateUniformImage(int width, int height)
    {
        ImageSize size = new(width, height);
        return new RgbaImage(
            size,
            Enumerable.Repeat(Rgba32.Opaque(100, 120, 140), checked((int)size.PixelCount)).ToArray());
    }

    private static BackgroundSuppressionResult CreateSuppression(
        ImageSize size,
        float artisticValue,
        float suppressionValue,
        DetailMap effective)
    {
        DetailMap suppression = DetailMap.CreateUniform(size, suppressionValue);
        DetailMap protection = DetailMap.CreateUniform(size, 1f - suppressionValue);
        return new BackgroundSuppressionResult(
            ArtisticDetailField.CreateUniform(size, artisticValue),
            suppression,
            protection,
            effective);
    }

    private static void AssertPlansEqual(StrokePlan expected, StrokePlan actual)
    {
        Assert.Equal(expected.PlannerVersion, actual.PlannerVersion);
        Assert.Equal(expected.FieldSeed, actual.FieldSeed);
        Assert.Equal(expected.Strokes.Count, actual.Strokes.Count);
        for (int index = 0; index < expected.Strokes.Count; index++)
        {
            Assert.Equal(expected.Strokes[index].Color, actual.Strokes[index].Color);
            Assert.Equal(
                expected.Strokes[index].WidthRelativeToReference,
                actual.Strokes[index].WidthRelativeToReference,
                12);
            Assert.Equal(expected.Strokes[index].Points, actual.Strokes[index].Points);
        }
    }

    private static double PathLength(FlowStroke stroke)
    {
        double total = 0d;
        for (int index = 1; index < stroke.Points.Count; index++)
        {
            double x = stroke.Points[index].X - stroke.Points[index - 1].X;
            double y = stroke.Points[index].Y - stroke.Points[index - 1].Y;
            total += Math.Sqrt((x * x) + (y * y));
        }

        return total;
    }

    private sealed class ConstantFieldFactory : IFlowFieldFactory
    {
        private readonly double _angle;

        public ConstantFieldFactory(double angle)
        {
            _angle = angle;
        }

        public IFlowField Create(int seed, FlowFieldSettings settings)
        {
            return new ConstantField(_angle);
        }
    }

    private sealed class ConstantField : IFlowField
    {
        private readonly double _angle;

        public ConstantField(double angle)
        {
            _angle = angle;
        }

        public double SampleAngle(double x, double y)
        {
            return _angle;
        }
    }
}
