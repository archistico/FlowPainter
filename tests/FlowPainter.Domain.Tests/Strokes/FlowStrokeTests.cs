using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Domain.Tests.Strokes;

public sealed class FlowStrokeTests
{
    [Fact]
    public void ConstructorCopiesPointCollection()
    {
        List<RelativePoint> points = [new RelativePoint(0d, 0d), new RelativePoint(1d, 1d)];
        FlowStroke stroke = new(0, points, Rgba32.Opaque(1, 2, 3), 0.01d);

        points[0] = new RelativePoint(0.5d, 0.5d);

        Assert.Equal(new RelativePoint(0d, 0d), stroke.Points[0]);
    }

    [Fact]
    public void ConstructorRejectsPathWithFewerThanTwoPoints()
    {
        Assert.Throws<ArgumentException>(
            () => new FlowStroke(
                0,
                [new RelativePoint(0d, 0d)],
                Rgba32.Opaque(1, 2, 3),
                0.01d));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidWidth(double width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FlowStroke(
                0,
                [new RelativePoint(0d, 0d), new RelativePoint(1d, 1d)],
                Rgba32.Opaque(1, 2, 3),
                width));
    }
}
