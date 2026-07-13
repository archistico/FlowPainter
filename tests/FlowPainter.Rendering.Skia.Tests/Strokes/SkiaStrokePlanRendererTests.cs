using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Strokes;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Tests.Strokes;

public sealed class SkiaStrokePlanRendererTests
{
    [Fact]
    public async Task RenderAsyncDrawsStrokeOnTransparentBackground()
    {
        ImageSize size = new(100, 100);
        StrokePlan plan = CreatePlan(
            size,
            [CreateStroke(0, new Rgba32(255, 0, 0, 255), 0.1d, (0.1d, 0.5d), (0.9d, 0.5d))],
            StrokePlanBackgroundMode.Transparent);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(plan, size);

        Rgba32 center = result.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        Rgba32 corner = result.SampleNearest(new NormalizedPoint(0d, 0d));
        Assert.True(center.Red > 200);
        Assert.True(center.Alpha > 200);
        Assert.Equal((byte)0, corner.Alpha);
    }

    [Fact]
    public async Task RenderAsyncCopiesSourceBackgroundWhenPlanHasNoStrokes()
    {
        ImageSize planSize = new(2, 1);
        StrokePlan plan = CreatePlan(planSize, [], StrokePlanBackgroundMode.SourceImage);
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            2,
            1,
            (x, _) => x == 0 ? SKColors.Red : SKColors.Blue);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(plan, new ImageSize(4, 2), source);

        Assert.Equal(Rgba32.Opaque(255, 0, 0), result.SampleNearest(new NormalizedPoint(0.1d, 0.5d)));
        Assert.Equal(Rgba32.Opaque(0, 0, 255), result.SampleNearest(new NormalizedPoint(0.9d, 0.5d)));
    }

    [Fact]
    public async Task RenderAsyncReturnsRequestedOutputDimensions()
    {
        StrokePlan plan = CreatePlan(new ImageSize(10, 5), [], StrokePlanBackgroundMode.Transparent);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(plan, new ImageSize(200, 100));

        Assert.Equal(new ImageSize(200, 100), result.Size);
    }

    [Fact]
    public async Task RenderAsyncScalesStrokeWidthWithOutputDimension()
    {
        StrokePlan plan = CreatePlan(
            new ImageSize(100, 100),
            [CreateStroke(0, Rgba32.Opaque(255, 255, 255), 0.05d, (0.1d, 0.5d), (0.9d, 0.5d))],
            StrokePlanBackgroundMode.Transparent);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage small = await renderer.RenderAsync(plan, new ImageSize(100, 100));
        using SkiaImage large = await renderer.RenderAsync(plan, new ImageSize(200, 200));

        Assert.True(small.SampleNearest(new NormalizedPoint(0.5d, 0.52d)).Alpha > 0);
        Assert.True(large.SampleNearest(new NormalizedPoint(0.5d, 0.52d)).Alpha > 0);
        Assert.Equal((byte)0, small.SampleNearest(new NormalizedPoint(0.5d, 0.56d)).Alpha);
        Assert.Equal((byte)0, large.SampleNearest(new NormalizedPoint(0.5d, 0.56d)).Alpha);
    }

    [Fact]
    public async Task RenderAsyncClipsLegacyOutOfBoundsPointsWithoutFailure()
    {
        StrokePlan plan = CreatePlan(
            new ImageSize(10, 10),
            [CreateStroke(0, Rgba32.Opaque(255, 255, 255), 0.1d, (-1d, 0.5d), (2d, 0.5d))],
            StrokePlanBackgroundMode.Transparent);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(plan, new ImageSize(50, 50));

        Assert.True(result.SampleNearest(new NormalizedPoint(0.5d, 0.5d)).Alpha > 0);
    }

    [Fact]
    public async Task RenderAsyncRequiresSourceForSourceImageBackground()
    {
        StrokePlan plan = CreatePlan(new ImageSize(10, 10), [], StrokePlanBackgroundMode.SourceImage);
        SkiaStrokePlanRenderer renderer = new();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => renderer.RenderAsync(plan, new ImageSize(10, 10)));
    }

    [Fact]
    public async Task RenderAsyncRejectsBackgroundWithDifferentAspectRatio()
    {
        StrokePlan plan = CreatePlan(new ImageSize(10, 10), [], StrokePlanBackgroundMode.SourceImage);
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(2, 1, (_, _) => SKColors.Black);
        SkiaStrokePlanRenderer renderer = new();

        await Assert.ThrowsAsync<ArgumentException>(
            () => renderer.RenderAsync(plan, new ImageSize(10, 10), source));
    }

    [Fact]
    public async Task RenderAsyncHonorsPreCancelledToken()
    {
        StrokePlan plan = CreatePlan(new ImageSize(10, 10), [], StrokePlanBackgroundMode.Transparent);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaStrokePlanRenderer renderer = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => renderer.RenderAsync(
                plan,
                new ImageSize(10, 10),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task RenderAsyncReportsCompletedProgress()
    {
        StrokePlan plan = CreatePlan(
            new ImageSize(10, 10),
            [CreateStroke(0, Rgba32.Opaque(255, 255, 255), 0.1d, (0d, 0d), (1d, 1d))],
            StrokePlanBackgroundMode.Transparent);
        RecordingProgress<StrokeRenderProgress> progress = new();
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(
            plan,
            new ImageSize(10, 10),
            progress: progress);

        Assert.Equal(StrokeRenderStage.Preparing, progress.Values[0].Stage);
        Assert.Equal(StrokeRenderStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
        Assert.Equal(1, progress.Values[^1].CompletedStrokes);
    }

    private static StrokePlan CreatePlan(
        ImageSize sourceSize,
        FlowStroke[] strokes,
        StrokePlanBackgroundMode backgroundMode)
    {
        return new StrokePlan(
            sourceSize,
            seed: 1UL,
            fieldSeed: 1,
            referenceMaximumDimension: 512,
            strokes,
            backgroundMode);
    }

    private static FlowStroke CreateStroke(
        int index,
        Rgba32 color,
        double width,
        params (double X, double Y)[] points)
    {
        return new FlowStroke(
            index,
            points.Select(point => new RelativePoint(point.X, point.Y)),
            color,
            width);
    }
}
