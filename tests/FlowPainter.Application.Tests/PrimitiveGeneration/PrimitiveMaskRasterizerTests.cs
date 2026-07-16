using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.PrimitiveGeneration;

public sealed class PrimitiveMaskRasterizerTests
{
    [Theory]
    [InlineData(PrimitiveKind.Triangle)]
    [InlineData(PrimitiveKind.Rectangle)]
    [InlineData(PrimitiveKind.RotatedRectangle)]
    [InlineData(PrimitiveKind.Circle)]
    [InlineData(PrimitiveKind.Ellipse)]
    public void RasterizeIncludesPixelsForEverySupportedKind(PrimitiveKind kind)
    {
        PrimitiveMaskRasterizer rasterizer = new();
        GeometricPrimitive primitive = new(
            0,
            kind,
            new NormalizedPoint(0.5d, 0.5d),
            0.5d,
            0.4d,
            0.3d,
            Rgba32.Opaque(0, 0, 0));

        PrimitiveRasterMask mask = rasterizer.Rasterize(primitive, new ImageSize(32, 24));

        Assert.NotEmpty(mask.PixelIndices);
        Assert.All(mask.PixelIndices, index => Assert.InRange(index, 0, (32 * 24) - 1));
    }

    [Fact]
    public void RasterizeClipsPrimitiveAtImageBoundary()
    {
        PrimitiveMaskRasterizer rasterizer = new();
        GeometricPrimitive primitive = new(
            0,
            PrimitiveKind.Circle,
            new NormalizedPoint(0d, 0d),
            0.5d,
            0.5d,
            0d,
            Rgba32.Opaque(0, 0, 0));

        PrimitiveRasterMask mask = rasterizer.Rasterize(primitive, new ImageSize(20, 20));

        Assert.NotEmpty(mask.PixelIndices);
        Assert.All(mask.PixelIndices, index => Assert.InRange(index, 0, 399));
    }
}
