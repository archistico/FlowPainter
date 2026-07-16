using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Strokes;
using FlowPainter.Rendering.Skia.Hybrid;

namespace FlowPainter.Rendering.Skia.Tests.Hybrid;

public sealed class SkiaHybridPlanRendererTests
{
    [Fact]
    public async Task RenderAsyncCombinesPrimitiveAndStrokeLayers()
    {
        HybridPlan plan = CreatePlan();
        SkiaHybridPlanRenderer renderer = new();

        using FlowPainter.Imaging.Skia.Images.SkiaImage image = await renderer.RenderAsync(
            plan,
            new ImageSize(64, 64),
            new BrushSettings(),
            new BrushSettings());

        Rgba32 center = image.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        Rgba32 corner = image.SampleNearest(new NormalizedPoint(0.02d, 0.02d));
        Assert.NotEqual(corner, center);
    }

    [Fact]
    public async Task RenderAsyncIsDeterministic()
    {
        HybridPlan plan = CreatePlan();
        SkiaHybridPlanRenderer renderer = new();

        using FlowPainter.Imaging.Skia.Images.SkiaImage first = await renderer.RenderAsync(
            plan,
            new ImageSize(48, 48),
            new BrushSettings(),
            new BrushSettings());
        using FlowPainter.Imaging.Skia.Images.SkiaImage second = await renderer.RenderAsync(
            plan,
            new ImageSize(48, 48),
            new BrushSettings(),
            new BrushSettings());

        Assert.Equal(first.EncodePng(), second.EncodePng());
    }

    [Fact]
    public async Task RenderAsyncReportsCompletion()
    {
        RecordingProgress progress = new();

        using FlowPainter.Imaging.Skia.Images.SkiaImage image = await new SkiaHybridPlanRenderer().RenderAsync(
            CreatePlan(),
            new ImageSize(32, 32),
            new BrushSettings(),
            new BrushSettings(),
            progress);

        Assert.Equal(HybridRenderStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction, 12);
    }

    [Fact]
    public async Task RenderAsyncHonorsCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SkiaHybridPlanRenderer().RenderAsync(
            CreatePlan(),
            new ImageSize(32, 32),
            new BrushSettings(),
            new BrushSettings(),
            cancellationToken: cancellation.Token));
    }

    private static HybridPlan CreatePlan()
    {
        ImageSize size = new(16, 16);
        GeometricPrimitive primitive = new(
            0,
            PrimitiveKind.Rectangle,
            new NormalizedPoint(0.5d, 0.5d),
            0.75d,
            0.75d,
            0d,
            Rgba32.Opaque(180, 80, 40));
        PrimitivePlan primitivePlan = new(
            size,
            1UL,
            Rgba32.Opaque(20, 30, 50),
            [primitive],
            "test");
        StrokePlan flow = CreateStrokePlan(size, 2UL, Rgba32.Opaque(250, 230, 180), 0.04d);
        StrokePlan refinement = CreateStrokePlan(size, 3UL, Rgba32.Opaque(20, 20, 20), 0.015d);
        return new HybridPlan(4UL, primitivePlan, flow, refinement);
    }

    private static StrokePlan CreateStrokePlan(
        ImageSize size,
        ulong seed,
        Rgba32 color,
        double width)
    {
        FlowStroke stroke = new(
            0,
            [new RelativePoint(0.1d, 0.5d), new RelativePoint(0.9d, 0.5d)],
            color,
            width);
        return new StrokePlan(
            size,
            seed,
            1,
            512,
            [stroke],
            StrokePlanBackgroundMode.SourceImage,
            "hybrid-test");
    }

    private sealed class RecordingProgress : IProgress<HybridRenderProgress>
    {
        public List<HybridRenderProgress> Values { get; } = [];

        public void Report(HybridRenderProgress value)
        {
            Values.Add(value);
        }
    }
}
