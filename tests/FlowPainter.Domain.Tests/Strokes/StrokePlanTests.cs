using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Domain.Tests.Strokes;

public sealed class StrokePlanTests
{
    [Fact]
    public void ConstructorCopiesStrokeCollection()
    {
        List<FlowStroke> strokes = [CreateStroke(0)];
        StrokePlan plan = new(new ImageSize(10, 10), 42UL, 7, 512, strokes);

        strokes.Clear();

        Assert.Single(plan.Strokes);
        Assert.Equal(StrokePlan.LegacyPlannerVersion, plan.PlannerVersion);
    }

    [Fact]
    public void ConstructorRejectsNonContiguousStrokeIndices()
    {
        Assert.Throws<ArgumentException>(
            () => new StrokePlan(
                new ImageSize(10, 10),
                42UL,
                7,
                512,
                [CreateStroke(1)]));
    }

    [Fact]
    public void ConstructorTrimsPlannerVersion()
    {
        StrokePlan plan = new(
            new ImageSize(10, 10),
            42UL,
            7,
            512,
            [CreateStroke(0)],
            plannerVersion: "  custom-v1  ");

        Assert.Equal("custom-v1", plan.PlannerVersion);
    }

    private static FlowStroke CreateStroke(int index)
    {
        return new FlowStroke(
            index,
            [new RelativePoint(0d, 0d), new RelativePoint(1d, 1d)],
            Rgba32.Opaque(1, 2, 3),
            0.01d);
    }
}
