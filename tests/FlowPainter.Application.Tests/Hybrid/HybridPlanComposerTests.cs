using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.Hybrid;

public sealed class HybridPlanComposerTests
{
    [Fact]
    public void CreatePlanBuildsThreeResolutionIndependentLayers()
    {
        RgbaImage image = CreateImage();
        DetailMap detailMap = DetailMap.CreateUniform(image.Size, 0.6f);
        StrokeDensityMap densityMap = StrokeDensityMap.CreateUniform(image.Size, 8d);
        FlowPainterSettings flowSettings = new(strokeCount: 20, segmentCount: 4);
        PrimitiveGenerationSettings primitiveSettings = new(
            primitiveCount: 8,
            candidatesPerStep: 3,
            mutationIterations: 2);
        HybridGenerationSettings hybridSettings = new(
            primitiveBudgetFraction: 0.25d,
            flowBudgetFraction: 0.50d,
            refinementBudgetFraction: 0.25d);

        HybridPlan plan = new HybridPlanComposer().CreatePlan(
            image,
            densityMap,
            detailMap,
            42UL,
            flowSettings,
            primitiveSettings,
            hybridSettings);

        Assert.Equal(image.Size, plan.SourceSize);
        Assert.Equal(10, plan.FlowStrokePlan.Strokes.Count);
        Assert.Equal(5, plan.RefinementStrokePlan.Strokes.Count);
        Assert.InRange(plan.PrimitivePlan.Primitives.Count, 0, 2);
        Assert.Equal(StrokePlanBackgroundMode.SourceImage, plan.FlowStrokePlan.BackgroundMode);
        Assert.Equal(StrokePlanBackgroundMode.SourceImage, plan.RefinementStrokePlan.BackgroundMode);
    }

    [Fact]
    public void EqualInputsProduceEqualHybridPlans()
    {
        RgbaImage image = CreateImage();
        DetailMap detailMap = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap densityMap = StrokeDensityMap.CreateUniform(image.Size, 8d);
        FlowPainterSettings flowSettings = new(strokeCount: 12, segmentCount: 3);
        PrimitiveGenerationSettings primitiveSettings = new(
            primitiveCount: 4,
            candidatesPerStep: 2,
            mutationIterations: 1);
        HybridGenerationSettings hybridSettings = new();
        HybridPlanComposer composer = new();

        HybridPlan first = composer.CreatePlan(
            image,
            densityMap,
            detailMap,
            123UL,
            flowSettings,
            primitiveSettings,
            hybridSettings);
        HybridPlan second = composer.CreatePlan(
            image,
            densityMap,
            detailMap,
            123UL,
            flowSettings,
            primitiveSettings,
            hybridSettings);

        AssertPrimitivePlansEqual(first.PrimitivePlan, second.PrimitivePlan);
        AssertStrokePlansEqual(first.FlowStrokePlan, second.FlowStrokePlan);
        AssertStrokePlansEqual(first.RefinementStrokePlan, second.RefinementStrokePlan);
    }

    [Fact]
    public void CreatePlanHonorsCancellation()
    {
        RgbaImage image = CreateImage();
        DetailMap detailMap = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap densityMap = StrokeDensityMap.CreateUniform(image.Size, 8d);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => new HybridPlanComposer().CreatePlan(
            image,
            densityMap,
            detailMap,
            1UL,
            new FlowPainterSettings(strokeCount: 4),
            new PrimitiveGenerationSettings(primitiveCount: 2),
            new HybridGenerationSettings(),
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void CreatePlanReportsAllMajorStages()
    {
        RgbaImage image = CreateImage();
        DetailMap detailMap = DetailMap.CreateUniform(image.Size, 0.5f);
        StrokeDensityMap densityMap = StrokeDensityMap.CreateUniform(image.Size, 8d);
        RecordingProgress progress = new();

        new HybridPlanComposer().CreatePlan(
            image,
            densityMap,
            detailMap,
            2UL,
            new FlowPainterSettings(strokeCount: 8, segmentCount: 2),
            new PrimitiveGenerationSettings(primitiveCount: 3, candidatesPerStep: 2, mutationIterations: 1),
            new HybridGenerationSettings(),
            progress);

        Assert.Contains(progress.Values, value => value.Stage == HybridPlanningStage.GeneratingPrimitives);
        Assert.Contains(progress.Values, value => value.Stage == HybridPlanningStage.PlanningFlowStrokes);
        Assert.Contains(progress.Values, value => value.Stage == HybridPlanningStage.PlanningRefinementStrokes);
        Assert.Equal(HybridPlanningStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction, 12);
    }


    private static void AssertPrimitivePlansEqual(
        FlowPainter.Domain.Primitives.PrimitivePlan expected,
        FlowPainter.Domain.Primitives.PrimitivePlan actual)
    {
        Assert.Equal(expected.BackgroundColor, actual.BackgroundColor);
        Assert.Equal(expected.Primitives.Count, actual.Primitives.Count);
        for (int index = 0; index < expected.Primitives.Count; index++)
        {
            Assert.Equal(expected.Primitives[index], actual.Primitives[index]);
        }
    }

    private static void AssertStrokePlansEqual(StrokePlan expected, StrokePlan actual)
    {
        Assert.Equal(expected.Strokes.Count, actual.Strokes.Count);
        for (int index = 0; index < expected.Strokes.Count; index++)
        {
            Assert.Equal(expected.Strokes[index].Color, actual.Strokes[index].Color);
            Assert.Equal(expected.Strokes[index].WidthRelativeToReference, actual.Strokes[index].WidthRelativeToReference, 12);
            Assert.Equal(expected.Strokes[index].Points, actual.Strokes[index].Points);
        }
    }

    private static RgbaImage CreateImage()
    {
        ImageSize size = new(8, 8);
        Rgba32[] pixels = new Rgba32[64];
        for (int y = 0; y < size.Height; y++)
        {
            for (int x = 0; x < size.Width; x++)
            {
                bool bright = ((x / 2) + (y / 2)) % 2 == 0;
                pixels[(y * size.Width) + x] = bright
                    ? Rgba32.Opaque(230, 180, 80)
                    : Rgba32.Opaque(30, 70, 150);
            }
        }

        return new RgbaImage(size, pixels);
    }

    private sealed class RecordingProgress : IProgress<HybridPlanningProgress>
    {
        public List<HybridPlanningProgress> Values { get; } = [];

        public void Report(HybridPlanningProgress value)
        {
            Values.Add(value);
        }
    }
}
