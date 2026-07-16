using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Workloads;

public sealed class WorkloadBudgetIntegrationTests
{
    [Fact]
    public void FlowPlannerRejectsUnsafeCompositeWorkBeforePlanning()
    {
        RgbaImage image = CreateImage();
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        FlowPainterPlanner planner = new(new DefaultFlowFieldFactory());
        FlowPainterSettings settings = new(
            strokeCount: FlowPainterSettings.MaximumStrokeCount,
            segmentCount: FlowPainterSettings.MaximumSegmentCount);

        Assert.Throws<InvalidOperationException>(
            () => planner.CreatePlan(image, density, 1UL, settings));
    }

    [Fact]
    public void PrimitiveOptimizerRejectsUnsafeCompositeWorkBeforePixelCopies()
    {
        RgbaImage image = CreateImage();
        PrimitivePlanOptimizer optimizer = new();
        PrimitiveGenerationSettings settings = new(
            primitiveCount: PrimitiveGenerationSettings.MaximumPrimitiveCount,
            candidatesPerStep: PrimitiveGenerationSettings.MaximumCandidateCount,
            mutationIterations: PrimitiveGenerationSettings.MaximumMutationIterations,
            minimumSize: 0.1d,
            maximumSize: 1d,
            detailSearchInfluence: 4d);

        Assert.Throws<InvalidOperationException>(
            () => optimizer.CreatePlan(image, detailMap: null, 1UL, settings));
    }

    [Fact]
    public void HybridComposerRejectsUnsafeCombinedWorkBeforeLayerPlanning()
    {
        RgbaImage image = CreateImage();
        DetailMap detail = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap density = StrokeDensityMap.CreateUniform(image.Size, 1d);
        HybridPlanComposer composer = new();
        FlowPainterSettings flow = new(
            strokeCount: FlowPainterSettings.MaximumStrokeCount,
            segmentCount: FlowPainterSettings.MaximumSegmentCount);

        Assert.Throws<InvalidOperationException>(() => composer.CreatePlan(
            image,
            density,
            detail,
            1UL,
            flow,
            new PrimitiveGenerationSettings(),
            new HybridGenerationSettings()));
    }

    private static RgbaImage CreateImage()
    {
        return new RgbaImage(
            new ImageSize(1, 1),
            [Rgba32.Opaque(20, 30, 40)]);
    }
}
