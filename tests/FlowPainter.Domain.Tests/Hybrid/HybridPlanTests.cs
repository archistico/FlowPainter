using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Domain.Tests.Hybrid;

public sealed class HybridPlanTests
{
    [Fact]
    public void ConstructorPreservesAllLayers()
    {
        PrimitivePlan primitivePlan = CreatePrimitivePlan(new ImageSize(8, 6));
        StrokePlan flowPlan = CreateStrokePlan(primitivePlan.SourceSize, 10UL);
        StrokePlan refinementPlan = CreateStrokePlan(primitivePlan.SourceSize, 11UL);

        HybridPlan plan = new(9UL, primitivePlan, flowPlan, refinementPlan, "  test-v1  ");

        Assert.Equal(9UL, plan.Seed);
        Assert.Same(primitivePlan, plan.PrimitivePlan);
        Assert.Same(flowPlan, plan.FlowStrokePlan);
        Assert.Same(refinementPlan, plan.RefinementStrokePlan);
        Assert.Equal(new ImageSize(8, 6), plan.SourceSize);
        Assert.Equal("test-v1", plan.PlannerVersion);
    }

    [Fact]
    public void ConstructorRejectsMismatchedLayerDimensions()
    {
        PrimitivePlan primitivePlan = CreatePrimitivePlan(new ImageSize(8, 6));
        StrokePlan flowPlan = CreateStrokePlan(new ImageSize(7, 6), 10UL);
        StrokePlan refinementPlan = CreateStrokePlan(primitivePlan.SourceSize, 11UL);

        Assert.Throws<ArgumentException>(() => new HybridPlan(9UL, primitivePlan, flowPlan, refinementPlan));
    }

    [Fact]
    public void ConstructorRejectsTransparentStrokeLayer()
    {
        PrimitivePlan primitivePlan = CreatePrimitivePlan(new ImageSize(8, 6));
        StrokePlan flowPlan = CreateStrokePlan(
            primitivePlan.SourceSize,
            10UL,
            StrokePlanBackgroundMode.Transparent);
        StrokePlan refinementPlan = CreateStrokePlan(primitivePlan.SourceSize, 11UL);

        Assert.Throws<ArgumentException>(() => new HybridPlan(9UL, primitivePlan, flowPlan, refinementPlan));
    }

    [Fact]
    public void ConstructorRejectsBlankPlannerVersion()
    {
        PrimitivePlan primitivePlan = CreatePrimitivePlan(new ImageSize(8, 6));
        StrokePlan flowPlan = CreateStrokePlan(primitivePlan.SourceSize, 10UL);
        StrokePlan refinementPlan = CreateStrokePlan(primitivePlan.SourceSize, 11UL);

        Assert.Throws<ArgumentException>(() => new HybridPlan(9UL, primitivePlan, flowPlan, refinementPlan, " "));
    }

    private static PrimitivePlan CreatePrimitivePlan(ImageSize size)
    {
        GeometricPrimitive primitive = new(
            0,
            PrimitiveKind.Ellipse,
            new NormalizedPoint(0.5d, 0.5d),
            0.4d,
            0.3d,
            0d,
            Rgba32.Opaque(120, 80, 40));
        return new PrimitivePlan(size, 1UL, Rgba32.Opaque(20, 20, 20), [primitive], "primitive-test");
    }

    private static StrokePlan CreateStrokePlan(
        ImageSize size,
        ulong seed,
        StrokePlanBackgroundMode backgroundMode = StrokePlanBackgroundMode.SourceImage)
    {
        FlowStroke stroke = new(
            0,
            [new RelativePoint(0.2d, 0.2d), new RelativePoint(0.8d, 0.8d)],
            Rgba32.Opaque(200, 180, 160),
            0.01d);
        return new StrokePlan(size, seed, 1, 512, [stroke], backgroundMode, "stroke-test");
    }
}
