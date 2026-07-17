using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class FlowPainterPlannerTests
{
    private const ulong GoldenSeed = 0x0123456789ABCDEFUL;

    [Fact]
    public void CreatePlanProducesStableGoldenStroke()
    {
        RgbaImage image = new(new ImageSize(1, 1), [new Rgba32(10, 20, 30, 255)]);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 2d);
        RecordingFieldFactory factory = new(new ConstantField(0d));
        FlowPainterPlanner planner = new(factory);
        FlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 2,
            uniformDensity: 2d,
            lengthScale: 0.1d,
            maximumCurveRadians: 1d,
            minimumStrokeWidthPixels: 2d,
            maximumStrokeWidthPixels: 4d,
            strokeOpacity: 0.5d);

        StrokePlan plan = planner.CreatePlan(image, density, GoldenSeed, settings);

        Assert.Equal(GoldenSeed, plan.Seed);
        Assert.Equal(1_334_057_644, plan.FieldSeed);
        Assert.Equal(plan.FieldSeed, factory.ReceivedSeed);
        Assert.Equal(FlowPainterPlanner.PlannerVersion, plan.PlannerVersion);
        FlowStroke stroke = Assert.Single(plan.Strokes);
        Assert.Equal(new Rgba32(10, 20, 30, 128), stroke.Color);
        Assert.InRange(
            Math.Abs(0.006390815880238687d - stroke.WidthRelativeToReference),
            0d,
            1e-12d);
        Assert.Collection(
            stroke.Points,
            point => AssertPoint(point, 0.8337909344596774d, 0.18580193412474622d),
            point => AssertPoint(point, 0.9337909344596774d, 0.18580193412474622d),
            point => AssertPoint(point, 1d, 0.18580193412474622d));
    }

    [Fact]
    public void CreatePlanIsRepeatableForEqualInputs()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 12d);
        FlowPainterSettings settings = new(strokeCount: 20, segmentCount: 8);
        FlowPainterPlanner planner = new(new DefaultFlowFieldFactory());

        StrokePlan first = planner.CreatePlan(image, density, 42UL, settings);
        StrokePlan second = planner.CreatePlan(image, density, 42UL, settings);

        Assert.Equal(first.FieldSeed, second.FieldSeed);
        Assert.Equal(first.Strokes.Count, second.Strokes.Count);
        for (int index = 0; index < first.Strokes.Count; index++)
        {
            Assert.Equal(first.Strokes[index].Color, second.Strokes[index].Color);
            Assert.Equal(first.Strokes[index].WidthRelativeToReference, second.Strokes[index].WidthRelativeToReference);
            Assert.Equal(first.Strokes[index].Points.Count, second.Strokes[index].Points.Count);
            for (int pointIndex = 0; pointIndex < first.Strokes[index].Points.Count; pointIndex++)
            {
                Assert.Equal(first.Strokes[index].Points[pointIndex], second.Strokes[index].Points[pointIndex]);
            }
        }
    }

    [Fact]
    public void CreatePlanStopsAtCanvasBoundary()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1_000d);
        FlowPainterPlanner planner = new(new RecordingFieldFactory(new ConstantField(0d)));
        FlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 20,
            uniformDensity: 1_000d,
            lengthScale: 0.1d);

        StrokePlan plan = planner.CreatePlan(image, density, 1UL, settings);

        Assert.All(plan.Strokes[0].Points, point => Assert.True(point.IsInsideCanvas));
        Assert.Equal(1d, plan.Strokes[0].Points[^1].X);
    }

    [Fact]
    public void CreatePlanStopsWhenCurveExceedsLimit()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        FlowPainterPlanner planner = new(
            new RecordingFieldFactory(new SequencedField([0d, Math.PI])));
        FlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 2,
            maximumCurveRadians: 0.5d);

        StrokePlan plan = planner.CreatePlan(image, density, 1UL, settings);

        Assert.Equal(2, plan.Strokes[0].Points.Count);
    }

    [Fact]
    public void CreatePlanUsesConfiguredBackgroundMode()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        FlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 1,
            backgroundMode: StrokePlanBackgroundMode.Transparent);

        StrokePlan plan = new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
            .CreatePlan(image, density, 1UL, settings);

        Assert.Equal(StrokePlanBackgroundMode.Transparent, plan.BackgroundMode);
    }

    [Fact]
    public void CreatePlanReportsProgressInOrder()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        RecordingProgress progress = new();
        FlowPainterSettings settings = new(strokeCount: 300, segmentCount: 1);

        _ = new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
            .CreatePlan(image, density, 1UL, settings, progress);

        Assert.Equal(StrokePlanningStage.Preparing, progress.Values[0].Stage);
        Assert.Contains(progress.Values, value => value.Stage == StrokePlanningStage.PlanningStrokes);
        Assert.Equal(StrokePlanningStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public void CreatePlanHonorsPreCancelledToken()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
                .CreatePlan(image, density, 1UL, new FlowPainterSettings(strokeCount: 1), cancellationToken: cancellation.Token));
    }

    [Fact]
    public void CreatePlanRejectsMismatchedDensityDimensions()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(new ImageSize(1, 1), 1d);

        Assert.Throws<ArgumentException>(
            () => new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
                .CreatePlan(image, density, 1UL, new FlowPainterSettings(strokeCount: 1)));
    }

    [Fact]
    public void CreatePlanRejectsNonFiniteFieldAngle()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);

        Assert.Throws<InvalidOperationException>(
            () => new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(double.NaN)))
                .CreatePlan(image, density, 1UL, new FlowPainterSettings(strokeCount: 1)));
    }


    [Fact]
    public void CreatePlanWithDetailMapUsesDetailPlannerVersion()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);

        StrokePlan plan = new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
            .CreatePlan(image, density, detail, 1UL, new FlowPainterSettings(strokeCount: 1));

        Assert.Equal(FlowPainterPlanner.DetailPlannerVersion, plan.PlannerVersion);
    }

    [Fact]
    public void CreatePlanWithDetailMapPlacesMoreStrokesInImportantArea()
    {
        RgbaImage image = new(
            new ImageSize(2, 1),
            [Rgba32.Opaque(10, 10, 10), Rgba32.Opaque(240, 240, 240)]);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        DetailMap detail = new(2, 1, [0f, 1f]);
        FlowPainterSettings settings = new(
            strokeCount: 2_000,
            segmentCount: 1,
            detailInfluence: new DetailInfluenceSettings(placementBias: 10d));

        StrokePlan plan = new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
            .CreatePlan(image, density, detail, 17UL, settings);

        int importantStarts = plan.Strokes.Count(stroke => stroke.Points[0].X >= 0.5d);
        Assert.InRange(importantStarts, 1_750, 2_000);
    }

    [Fact]
    public void CreatePlanWithDetailMapUsesShorterThinnerStrokesInDetailedArea()
    {
        RgbaImage image = new(new ImageSize(1, 1), [Rgba32.Opaque(100, 100, 100)]);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 4d);
        DetailInfluenceSettings influence = new(
            placementBias: 0d,
            detailedLengthMultiplier: 0.5d,
            backgroundLengthMultiplier: 1.5d,
            detailedWidthMultiplier: 0.5d,
            backgroundWidthMultiplier: 1.5d);
        FlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 1,
            lengthScale: 0.05d,
            minimumStrokeWidthPixels: 4d,
            maximumStrokeWidthPixels: 4d,
            detailInfluence: influence);
        FlowPainterPlanner planner = new(new RecordingFieldFactory(new CenterSeekingField()));

        StrokePlan background = planner.CreatePlan(
            image,
            density,
            DetailMap.CreateUniform(image.Size, 0f),
            99UL,
            settings);
        StrokePlan detailed = planner.CreatePlan(
            image,
            density,
            DetailMap.CreateUniform(image.Size, 1f),
            99UL,
            settings);

        double backgroundLength = Math.Abs(background.Strokes[0].Points[^1].X - background.Strokes[0].Points[0].X);
        double detailedLength = Math.Abs(detailed.Strokes[0].Points[^1].X - detailed.Strokes[0].Points[0].X);
        Assert.True(detailedLength < backgroundLength);
        Assert.True(detailed.Strokes[0].WidthRelativeToReference < background.Strokes[0].WidthRelativeToReference);
    }

    [Fact]
    public void CreatePlanWithDetailMapIsRepeatable()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        DetailMap detail = new(2, 2, [0f, 0.25f, 0.75f, 1f]);
        FlowPainterSettings settings = new(strokeCount: 50, segmentCount: 4);
        FlowPainterPlanner planner = new(new DefaultFlowFieldFactory());

        StrokePlan first = planner.CreatePlan(image, density, detail, 123UL, settings);
        StrokePlan second = planner.CreatePlan(image, density, detail, 123UL, settings);

        Assert.Equal(first.Strokes.Count, second.Strokes.Count);
        for (int index = 0; index < first.Strokes.Count; index++)
        {
            Assert.Equal(first.Strokes[index].Color, second.Strokes[index].Color);
            Assert.Equal(first.Strokes[index].WidthRelativeToReference, second.Strokes[index].WidthRelativeToReference);
            Assert.Equal(first.Strokes[index].Points.ToArray(), second.Strokes[index].Points.ToArray());
        }
    }

    [Fact]
    public void CreatePlanWithNeutralDetailInfluencePreservesBaseStrokeSequence()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        DetailMap detail = new(2, 2, [0f, 0.25f, 0.75f, 1f]);
        DetailInfluenceSettings neutralInfluence = new(
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
            detailedCrossingResistanceBoost: 0d);
        FlowPainterSettings settings = new(
            strokeCount: 50,
            segmentCount: 4,
            detailInfluence: neutralInfluence);
        FlowPainterPlanner planner = new(new DefaultFlowFieldFactory());

        StrokePlan basePlan = planner.CreatePlan(image, density, 123UL, settings);
        StrokePlan detailPlan = planner.CreatePlan(image, density, detail, 123UL, settings);

        Assert.Equal(basePlan.FieldSeed, detailPlan.FieldSeed);
        Assert.Equal(basePlan.Strokes.Count, detailPlan.Strokes.Count);
        for (int index = 0; index < basePlan.Strokes.Count; index++)
        {
            Assert.Equal(basePlan.Strokes[index].Color, detailPlan.Strokes[index].Color);
            Assert.Equal(basePlan.Strokes[index].WidthRelativeToReference, detailPlan.Strokes[index].WidthRelativeToReference);
            Assert.Equal(basePlan.Strokes[index].Points.ToArray(), detailPlan.Strokes[index].Points.ToArray());
        }
    }

    [Fact]
    public void CreatePlanRejectsMismatchedDetailDimensions()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        DetailMap detail = DetailMap.CreateUniform(new ImageSize(1, 1), 0.5f);

        Assert.Throws<ArgumentException>(
            () => new FlowPainterPlanner(new RecordingFieldFactory(new ConstantField(0d)))
                .CreatePlan(image, density, detail, 1UL, new FlowPainterSettings(strokeCount: 1)));
    }

    private static RgbaImage CreateImage()
    {
        return new RgbaImage(
            new ImageSize(2, 2),
            [
                Rgba32.Opaque(10, 20, 30),
                Rgba32.Opaque(40, 50, 60),
                Rgba32.Opaque(70, 80, 90),
                Rgba32.Opaque(100, 110, 120)
            ]);
    }

    private static void AssertPoint(RelativePoint point, double expectedX, double expectedY)
    {
        Assert.InRange(Math.Abs(expectedX - point.X), 0d, 1e-12d);
        Assert.InRange(Math.Abs(expectedY - point.Y), 0d, 1e-12d);
    }

    private sealed class RecordingFieldFactory : IFlowFieldFactory
    {
        private readonly IFlowField _field;

        public RecordingFieldFactory(IFlowField field)
        {
            _field = field;
        }

        public int ReceivedSeed { get; private set; } = -1;

        public IFlowField Create(int seed, FlowFieldSettings settings)
        {
            ReceivedSeed = seed;
            return _field;
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

    private sealed class CenterSeekingField : IFlowField
    {
        public double SampleAngle(double x, double y)
        {
            return x < 0.5d ? 0d : Math.PI;
        }
    }

    private sealed class SequencedField : IFlowField
    {
        private readonly Queue<double> _angles;

        public SequencedField(IEnumerable<double> angles)
        {
            _angles = new Queue<double>(angles);
        }

        public double SampleAngle(double x, double y)
        {
            return _angles.Count == 0 ? 0d : _angles.Dequeue();
        }
    }

    private sealed class RecordingProgress : IProgress<StrokePlanningProgress>
    {
        public List<StrokePlanningProgress> Values { get; } = [];

        public void Report(StrokePlanningProgress value)
        {
            Values.Add(value);
        }
    }
}
