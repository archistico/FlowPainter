using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Strokes;

namespace FlowPainter.Rendering.Skia.Tests.Brushes;

public sealed class SkiaBrushRenderingTests
{
    [Theory]
    [InlineData(BrushKind.SolidRound)]
    [InlineData(BrushKind.SoftRound)]
    [InlineData(BrushKind.Flat)]
    [InlineData(BrushKind.Bristle)]
    public async Task RenderAsyncDrawsEveryBuiltInBrush(BrushKind kind)
    {
        StrokePlan plan = CreatePlan(42UL);
        BrushSettings brush = new(kind, hardness: 0.45d, bristleCount: 7, bristleSpread: 0.85d);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage result = await renderer.RenderAsync(
            plan,
            new ImageSize(128, 128),
            brush: brush);

        Assert.True(result.SampleNearest(new NormalizedPoint(0.5d, 0.5d)).Alpha > 0);
    }

    [Fact]
    public async Task RenderAsyncDefaultBrushMatchesExplicitSolidRound()
    {
        StrokePlan plan = CreatePlan(42UL);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage implicitResult = await renderer.RenderAsync(plan, new ImageSize(128, 128));
        using SkiaImage explicitResult = await renderer.RenderAsync(
            plan,
            new ImageSize(128, 128),
            brush: new BrushSettings(BrushKind.SolidRound));

        Assert.True(implicitResult.EncodePng().SequenceEqual(explicitResult.EncodePng()));
    }

    [Fact]
    public async Task SoftRoundExtendsBeyondSolidRoundEdge()
    {
        StrokePlan plan = CreatePlan(42UL, width: 0.08d);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage solid = await renderer.RenderAsync(
            plan,
            new ImageSize(100, 100),
            brush: new BrushSettings(BrushKind.SolidRound));
        using SkiaImage soft = await renderer.RenderAsync(
            plan,
            new ImageSize(100, 100),
            brush: new BrushSettings(BrushKind.SoftRound, hardness: 0.1d));

        Assert.Equal((byte)0, solid.SampleNearest(new NormalizedPoint(0.5d, 0.56d)).Alpha);
        Assert.True(soft.SampleNearest(new NormalizedPoint(0.5d, 0.56d)).Alpha > 0);
    }

    [Fact]
    public async Task BrushKindsProduceDistinctRasterizations()
    {
        StrokePlan plan = CreateCornerPlan(42UL);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage solid = await renderer.RenderAsync(
            plan,
            new ImageSize(128, 128),
            brush: new BrushSettings(BrushKind.SolidRound));
        using SkiaImage flat = await renderer.RenderAsync(
            plan,
            new ImageSize(128, 128),
            brush: new BrushSettings(BrushKind.Flat));
        using SkiaImage bristle = await renderer.RenderAsync(
            plan,
            new ImageSize(128, 128),
            brush: new BrushSettings(BrushKind.Bristle, hardness: 0.4d, bristleCount: 5, bristleSpread: 0.9d));

        byte[] solidBytes = solid.EncodePng();
        Assert.False(solidBytes.SequenceEqual(flat.EncodePng()));
        Assert.False(solidBytes.SequenceEqual(bristle.EncodePng()));
    }

    [Fact]
    public async Task BristleRenderingIsDeterministicForEqualPlanAndSettings()
    {
        StrokePlan plan = CreateCornerPlan(12345UL);
        BrushSettings brush = new(
            BrushKind.Bristle,
            hardness: 0.35d,
            sizeJitter: 0.25d,
            opacityJitter: 0.3d,
            bristleCount: 9,
            bristleSpread: 0.9d);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage first = await renderer.RenderAsync(plan, new ImageSize(160, 160), brush: brush);
        using SkiaImage second = await renderer.RenderAsync(plan, new ImageSize(160, 160), brush: brush);

        Assert.True(first.EncodePng().SequenceEqual(second.EncodePng()));
    }

    [Fact]
    public async Task JitterChangesWhenPlanSeedChanges()
    {
        BrushSettings brush = new(
            BrushKind.Bristle,
            hardness: 0.35d,
            sizeJitter: 0.3d,
            opacityJitter: 0.35d,
            bristleCount: 9,
            bristleSpread: 0.9d);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage first = await renderer.RenderAsync(CreateCornerPlan(1UL), new ImageSize(160, 160), brush: brush);
        using SkiaImage second = await renderer.RenderAsync(CreateCornerPlan(2UL), new ImageSize(160, 160), brush: brush);

        Assert.False(first.EncodePng().SequenceEqual(second.EncodePng()));
    }

    [Fact]
    public async Task BristleCountChangesRasterization()
    {
        StrokePlan plan = CreateCornerPlan(42UL);
        SkiaStrokePlanRenderer renderer = new();

        using SkiaImage sparse = await renderer.RenderAsync(
            plan,
            new ImageSize(160, 160),
            brush: new BrushSettings(BrushKind.Bristle, hardness: 0.5d, bristleCount: 3, bristleSpread: 0.9d));
        using SkiaImage dense = await renderer.RenderAsync(
            plan,
            new ImageSize(160, 160),
            brush: new BrushSettings(BrushKind.Bristle, hardness: 0.5d, bristleCount: 13, bristleSpread: 0.9d));

        Assert.False(sparse.EncodePng().SequenceEqual(dense.EncodePng()));
    }

    private static StrokePlan CreatePlan(ulong seed, double width = 0.08d)
    {
        FlowStroke stroke = new(
            0,
            [new RelativePoint(0.15d, 0.5d), new RelativePoint(0.85d, 0.5d)],
            Rgba32.Opaque(220, 80, 40),
            width);
        return new StrokePlan(
            new ImageSize(128, 128),
            seed,
            fieldSeed: 1,
            referenceMaximumDimension: 512,
            [stroke],
            StrokePlanBackgroundMode.Transparent);
    }

    private static StrokePlan CreateCornerPlan(ulong seed)
    {
        FlowStroke stroke = new(
            0,
            [
                new RelativePoint(0.2d, 0.75d),
                new RelativePoint(0.5d, 0.25d),
                new RelativePoint(0.8d, 0.75d)
            ],
            Rgba32.Opaque(40, 170, 220),
            0.1d);
        return new StrokePlan(
            new ImageSize(128, 128),
            seed,
            fieldSeed: 1,
            referenceMaximumDimension: 512,
            [stroke],
            StrokePlanBackgroundMode.Transparent);
    }
}
