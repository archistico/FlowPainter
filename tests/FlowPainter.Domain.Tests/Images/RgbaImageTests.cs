using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Tests.Images;

public sealed class RgbaImageTests
{
    [Fact]
    public void ConstructorCopiesPixelBuffer()
    {
        Rgba32[] pixels = [Rgba32.Opaque(1, 2, 3)];
        RgbaImage image = new(new ImageSize(1, 1), pixels);

        pixels[0] = Rgba32.Opaque(9, 9, 9);

        Assert.Equal(Rgba32.Opaque(1, 2, 3), image[0, 0]);
    }

    [Fact]
    public void ConstructorRejectsIncorrectPixelCount()
    {
        Assert.Throws<ArgumentException>(
            () => new RgbaImage(new ImageSize(2, 2), [Rgba32.Opaque(1, 2, 3)]));
    }

    [Fact]
    public void SampleNearestMapsNormalizedCorners()
    {
        RgbaImage image = new(
            new ImageSize(2, 2),
            [
                Rgba32.Opaque(1, 0, 0),
                Rgba32.Opaque(2, 0, 0),
                Rgba32.Opaque(3, 0, 0),
                Rgba32.Opaque(4, 0, 0)
            ]);

        Assert.Equal(Rgba32.Opaque(1, 0, 0), image.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(Rgba32.Opaque(4, 0, 0), image.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public void IndexerRejectsCoordinatesOutsideImage()
    {
        RgbaImage image = new(new ImageSize(1, 1), [Rgba32.Opaque(1, 2, 3)]);

        Assert.Throws<ArgumentOutOfRangeException>(() => image[-1, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => image[0, 1]);
    }
}
