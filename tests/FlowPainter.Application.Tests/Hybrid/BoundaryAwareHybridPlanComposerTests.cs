using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Hybrid;

public sealed class BoundaryAwareHybridPlanComposerTests
{
    [Fact]
    public void CreatePlanUsesBoundaryAwarePlannerForBothStrokeLayers()
    {
        RgbaImage image = CreateImage();
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        SceneBoundaryAnalysisResult boundaries = CreateBoundaries(image.Size);
        FlowPainterSettings flowSettings = new(
            strokeCount: 12,
            segmentCount: 3,
            boundaryPainting: new BoundaryPaintingSettings(enabled: true));

        FlowPainter.Domain.Hybrid.HybridPlan plan = new HybridPlanComposer().CreatePlan(
            image,
            density,
            detail,
            boundaries,
            17UL,
            flowSettings,
            new PrimitiveGenerationSettings(
                primitiveCount: 4,
                candidatesPerStep: 2,
                mutationIterations: 1),
            new HybridGenerationSettings());

        Assert.Equal(FlowPainterPlanner.BoundaryPlannerVersion, plan.FlowStrokePlan.PlannerVersion);
        Assert.Equal(FlowPainterPlanner.BoundaryPlannerVersion, plan.RefinementStrokePlan.PlannerVersion);
    }

    [Fact]
    public void CreatePlanRequiresBoundaryAnalysisWhenGuidanceIsEnabled()
    {
        RgbaImage image = CreateImage();
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 8d);
        FlowPainterSettings flowSettings = new(
            strokeCount: 8,
            segmentCount: 2,
            boundaryPainting: new BoundaryPaintingSettings(enabled: true));

        Assert.Throws<ArgumentNullException>(() => new HybridPlanComposer().CreatePlan(
            image,
            density,
            detail,
            4UL,
            flowSettings,
            new PrimitiveGenerationSettings(
                primitiveCount: 2,
                candidatesPerStep: 1,
                mutationIterations: 1),
            new HybridGenerationSettings()));
    }

    private static RgbaImage CreateImage()
    {
        ImageSize size = new(8, 8);
        return new RgbaImage(
            size,
            Enumerable.Repeat(Rgba32.Opaque(120, 90, 70), checked((int)size.PixelCount)).ToArray());
    }

    private static SceneBoundaryAnalysisResult CreateBoundaries(ImageSize size)
    {
        DetailMap full = DetailMap.CreateUniform(size, 1f);
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        BoundaryVector[] directions = Enumerable.Repeat(
            new BoundaryVector(0d, 1d),
            checked((int)size.PixelCount)).ToArray();
        return new SceneBoundaryAnalysisResult(
            full,
            full,
            full,
            empty,
            empty,
            empty,
            empty,
            new BoundaryDirectionField(size.Width, size.Height, directions),
            "test-boundaries");
    }
}
