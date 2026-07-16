using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Primitives;
using FlowPainter.Rendering.Skia.Tests.Strokes;

namespace FlowPainter.Rendering.Skia.Tests.Primitives;

public sealed class SkiaPrimitivePlanRendererTests
{
    [Theory]
    [InlineData(PrimitiveKind.Triangle)]
    [InlineData(PrimitiveKind.Rectangle)]
    [InlineData(PrimitiveKind.RotatedRectangle)]
    [InlineData(PrimitiveKind.Circle)]
    [InlineData(PrimitiveKind.Ellipse)]
    public async Task RenderAsyncDrawsEveryPrimitiveKind(PrimitiveKind kind)
    {
        PrimitivePlan plan = CreatePlan(kind);
        SkiaPrimitivePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(plan, new ImageSize(100, 80));

        Rgba32 center = result.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        Assert.True(center.Red > center.Blue);
        Assert.Equal(new ImageSize(100, 80), result.Size);
    }

    [Fact]
    public async Task RenderAsyncUsesPlanBackground()
    {
        PrimitivePlan plan = new(
            new ImageSize(10, 10),
            1UL,
            Rgba32.Opaque(12, 34, 56),
            [],
            "test");
        SkiaPrimitivePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(plan, new ImageSize(20, 20));

        Assert.Equal(Rgba32.Opaque(12, 34, 56), result.SampleNearest(new NormalizedPoint(0d, 0d)));
    }

    [Fact]
    public async Task RenderAsyncHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaPrimitivePlanRenderer renderer = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => renderer.RenderAsync(
            CreatePlan(PrimitiveKind.Circle),
            new ImageSize(20, 20),
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task RenderAsyncReportsCompletedProgress()
    {
        RecordingProgress<PrimitiveRenderProgress> progress = new();
        SkiaPrimitivePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(
            CreatePlan(PrimitiveKind.Rectangle),
            new ImageSize(20, 20),
            progress);

        Assert.Equal(PrimitiveRenderStage.Preparing, progress.Values[0].Stage);
        Assert.Equal(PrimitiveRenderStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    private static PrimitivePlan CreatePlan(PrimitiveKind kind)
    {
        GeometricPrimitive primitive = new(
            0,
            kind,
            new NormalizedPoint(0.5d, 0.5d),
            0.6d,
            kind == PrimitiveKind.Circle ? 0.6d : 0.4d,
            kind == PrimitiveKind.Rectangle ? 0d : 0.3d,
            new Rgba32(240, 20, 10, 230));
        return new PrimitivePlan(
            new ImageSize(20, 20),
            5UL,
            Rgba32.Opaque(10, 20, 80),
            [primitive],
            "test");
    }
}
