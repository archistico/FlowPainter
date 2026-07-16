using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Domain.Tests.Primitives;

public sealed class GeometricPrimitiveTests
{
    [Fact]
    public void ConstructorNormalizesRotationAndPreservesGeometry()
    {
        GeometricPrimitive primitive = new(
            2,
            PrimitiveKind.Ellipse,
            new NormalizedPoint(0.25d, 0.75d),
            0.4d,
            0.2d,
            AngleMath.Tau + 0.5d,
            Rgba32.Opaque(10, 20, 30));

        Assert.Equal(2, primitive.Index);
        Assert.Equal(PrimitiveKind.Ellipse, primitive.Kind);
        Assert.Equal(0.5d, primitive.RotationRadians, 12);
        Assert.Equal(0.4d, primitive.Width, 12);
        Assert.Equal(0.2d, primitive.Height, 12);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidWidth(double width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeometricPrimitive(
            0,
            PrimitiveKind.Rectangle,
            new NormalizedPoint(0.5d, 0.5d),
            width,
            0.2d,
            0d,
            Rgba32.Opaque(0, 0, 0)));
    }

    [Fact]
    public void WithColorCreatesIndependentPrimitive()
    {
        GeometricPrimitive original = new(
            0,
            PrimitiveKind.Circle,
            new NormalizedPoint(0.5d, 0.5d),
            0.2d,
            0.2d,
            0d,
            Rgba32.Opaque(0, 0, 0));

        GeometricPrimitive changed = original.WithColor(new Rgba32(50, 60, 70, 128));

        Assert.NotSame(original, changed);
        Assert.Equal(original.Center, changed.Center);
        Assert.Equal(new Rgba32(50, 60, 70, 128), changed.Color);
        Assert.Equal(Rgba32.Opaque(0, 0, 0), original.Color);
    }
}
