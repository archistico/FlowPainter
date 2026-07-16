using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Domain.Tests.Primitives;

public sealed class PrimitivePlanTests
{
    [Fact]
    public void ConstructorCopiesPrimitiveCollection()
    {
        List<GeometricPrimitive> primitives = [CreatePrimitive(0)];
        PrimitivePlan plan = new(
            new ImageSize(10, 10),
            42UL,
            Rgba32.Opaque(5, 6, 7),
            primitives,
            "test-v1");

        primitives.Clear();

        Assert.Single(plan.Primitives);
        Assert.Equal("test-v1", plan.PlannerVersion);
        Assert.Equal(42UL, plan.Seed);
    }

    [Fact]
    public void ConstructorRejectsNonContiguousIndexes()
    {
        GeometricPrimitive[] primitives = [CreatePrimitive(1)];

        Assert.Throws<ArgumentException>(() => new PrimitivePlan(
            new ImageSize(10, 10),
            0UL,
            Rgba32.Opaque(0, 0, 0),
            primitives,
            "test-v1"));
    }

    private static GeometricPrimitive CreatePrimitive(int index)
    {
        return new GeometricPrimitive(
            index,
            PrimitiveKind.Triangle,
            new NormalizedPoint(0.5d, 0.5d),
            0.2d,
            0.2d,
            0d,
            Rgba32.Opaque(10, 20, 30));
    }
}
