using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Tests.Boundaries;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class BoundaryAwareFlowPainterPlannerTests
{
    [Fact]
    public void DisabledBoundaryPaintingPreservesDetailPlanExactly()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult boundaries = CreateUniformBoundaryAnalysis(image.Size, new BoundaryVector(0d, 1d));
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(enabled: false));
        FlowPainterPlanner planner = new(new ConstantFieldFactory(0d));

        StrokePlan expected = planner.CreatePlan(image, density, detail, 27UL, settings);
        StrokePlan actual = planner.CreatePlan(image, density, detail, boundaries, 27UL, settings);

        Assert.Equal(FlowPainterPlanner.DetailPlannerVersion, actual.PlannerVersion);
        AssertStrokePlansEqual(expected, actual);
    }

    [Fact]
    public void TangentAlignmentMakesStrokesParallelToBoundary()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult boundaries = CreateUniformBoundaryAnalysis(image.Size, new BoundaryVector(0d, 1d));
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(
            enabled: true,
            tangentAlignment: 1d,
            alignmentRadius: 0,
            crossingPenalty: 0d,
            hardBoundaryThreshold: 1d,
            terminationStrength: 0d,
            contourReinforcement: 0d,
            cornerPreservation: 0d));

        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            density,
            detail,
            boundaries,
            2UL,
            settings);
        RelativePoint first = plan.Strokes[0].Points[0];
        RelativePoint second = plan.Strokes[0].Points[1];

        Assert.Equal(FlowPainterPlanner.BoundaryPlannerVersion, plan.PlannerVersion);
        Assert.InRange(Math.Abs(second.X - first.X), 0d, 1e-12d);
        Assert.True(Math.Abs(second.Y - first.Y) > 0.001d);
    }

    [Fact]
    public void HardBoundaryTerminatesStrokeAfterFirstSafeSegment()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult boundaries = CreateUniformBoundaryAnalysis(image.Size, new BoundaryVector(0d, 1d));
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(
            enabled: true,
            tangentAlignment: 0d,
            alignmentRadius: 0,
            crossingPenalty: 0d,
            hardBoundaryThreshold: 0.5d,
            terminationStrength: 1d,
            contourReinforcement: 0d,
            cornerPreservation: 0d));

        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            density,
            detail,
            boundaries,
            3UL,
            settings);

        Assert.Equal(2, plan.Strokes[0].Points.Count);
    }

    [Fact]
    public void TextureInfluenceCanRemainUnconstrained()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult textureBoundaries = CreateUniformBoundaryAnalysis(
            image.Size,
            new BoundaryVector(0d, 1d),
            edgeImportance: 0f,
            subjectBoundary: 0f,
            texture: 1f);
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(
            enabled: true,
            tangentAlignment: 1d,
            alignmentRadius: 0,
            crossingPenalty: 0d,
            hardBoundaryThreshold: 1d,
            terminationStrength: 0d,
            textureEdgeInfluence: 0d,
            contourReinforcement: 0d,
            cornerPreservation: 0d));

        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            density,
            detail,
            textureBoundaries,
            4UL,
            settings);
        RelativePoint first = plan.Strokes[0].Points[0];
        RelativePoint second = plan.Strokes[0].Points[1];

        Assert.True(Math.Abs(second.X - first.X) > 0.001d);
        Assert.InRange(Math.Abs(second.Y - first.Y), 0d, 1e-12d);
    }

    [Fact]
    public void TextureInfluenceCanGuideStrokeWhenEnabled()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult textureBoundaries = CreateUniformBoundaryAnalysis(
            image.Size,
            new BoundaryVector(0d, 1d),
            edgeImportance: 0f,
            subjectBoundary: 0f,
            texture: 1f);
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(
            enabled: true,
            tangentAlignment: 1d,
            alignmentRadius: 0,
            crossingPenalty: 0d,
            hardBoundaryThreshold: 1d,
            terminationStrength: 0d,
            textureEdgeInfluence: 1d,
            contourReinforcement: 0d,
            cornerPreservation: 0d));

        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            density,
            detail,
            textureBoundaries,
            4UL,
            settings);
        RelativePoint first = plan.Strokes[0].Points[0];
        RelativePoint second = plan.Strokes[0].Points[1];

        Assert.InRange(Math.Abs(second.X - first.X), 0d, 1e-12d);
        Assert.True(Math.Abs(second.Y - first.Y) > 0.001d);
    }

    [Fact]
    public void BoundaryAwarePlanIsDeterministic()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult boundaries = CreateUniformBoundaryAnalysis(image.Size, new BoundaryVector(0d, 1d));
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(enabled: true));
        FlowPainterPlanner planner = new(new ConstantFieldFactory(0.35d));

        StrokePlan first = planner.CreatePlan(image, density, detail, boundaries, 99UL, settings);
        StrokePlan second = planner.CreatePlan(image, density, detail, boundaries, 99UL, settings);

        AssertStrokePlansEqual(first, second);
    }

    [Fact]
    public void RegionalBoundaryOverloadPublishesRegionalPlannerVersion()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(
            enabled: true,
            alignmentRadius: 2));

        StrokePlan plan = new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            density,
            detail,
            SceneBoundaryAnalysisResult.CreateEmpty(image.Size),
            RegionalBoundaryTestFactory.CreateVerticalSplit(8, 8, 4, 0.85d),
            18UL,
            settings);

        Assert.Equal(FlowPainterPlanner.RegionalBoundaryPlannerVersion, plan.PlannerVersion);
    }

    [Fact]
    public void CreatePlanRejectsMismatchedBoundaryDimensions()
    {
        RgbaImage image = CreateUniformImage(8, 8);
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult boundaries = CreateUniformBoundaryAnalysis(
            new ImageSize(4, 4),
            new BoundaryVector(0d, 1d));
        FlowPainterSettings settings = CreateSettings(new BoundaryPaintingSettings(enabled: true));

        Assert.Throws<ArgumentException>(() => new FlowPainterPlanner(new ConstantFieldFactory(0d)).CreatePlan(
            image,
            density,
            detail,
            boundaries,
            1UL,
            settings));
    }

    private static FlowPainterSettings CreateSettings(BoundaryPaintingSettings boundaryPainting)
    {
        return new FlowPainterSettings(
            strokeCount: 1,
            segmentCount: 6,
            uniformDensity: 8d,
            lengthScale: 0.01d,
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
            boundaryPainting: boundaryPainting);
    }

    private static RgbaImage CreateUniformImage(int width, int height)
    {
        ImageSize size = new(width, height);
        return new RgbaImage(
            size,
            Enumerable.Repeat(Rgba32.Opaque(100, 120, 140), checked((int)size.PixelCount)).ToArray());
    }

    private static SceneBoundaryAnalysisResult CreateUniformBoundaryAnalysis(
        ImageSize size,
        BoundaryVector tangent,
        float edgeImportance = 1f,
        float subjectBoundary = 1f,
        float texture = 0f)
    {
        DetailMap edge = DetailMap.CreateUniform(size, edgeImportance);
        DetailMap subject = DetailMap.CreateUniform(size, subjectBoundary);
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        DetailMap textureMap = DetailMap.CreateUniform(size, texture);
        BoundaryVector[] tangents = Enumerable.Repeat(
            tangent,
            checked((int)size.PixelCount)).ToArray();
        return new SceneBoundaryAnalysisResult(
            edge,
            edge,
            subject,
            empty,
            textureMap,
            empty,
            empty,
            new BoundaryDirectionField(size.Width, size.Height, tangents),
            "test-boundaries");
    }

    private static void AssertStrokePlansEqual(StrokePlan expected, StrokePlan actual)
    {
        Assert.Equal(expected.PlannerVersion, actual.PlannerVersion);
        Assert.Equal(expected.FieldSeed, actual.FieldSeed);
        Assert.Equal(expected.Strokes.Count, actual.Strokes.Count);
        for (int index = 0; index < expected.Strokes.Count; index++)
        {
            Assert.Equal(expected.Strokes[index].Color, actual.Strokes[index].Color);
            Assert.Equal(expected.Strokes[index].WidthRelativeToReference, actual.Strokes[index].WidthRelativeToReference, 12);
            Assert.Equal(expected.Strokes[index].Points, actual.Strokes[index].Points);
        }
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
