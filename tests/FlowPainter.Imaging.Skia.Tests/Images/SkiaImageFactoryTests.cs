using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;

namespace FlowPainter.Imaging.Skia.Tests.Images;

public sealed class SkiaImageFactoryTests
{
    private static readonly Rgba32[] SourcePixels =
    [
        new Rgba32(10, 20, 30, 40),
        new Rgba32(100, 110, 120, 130),
        new Rgba32(200, 210, 220, 230),
        Rgba32.Opaque(250, 240, 230)
    ];

    [Fact]
    public void CreateCopiesDimensionsPixelsAndName()
    {
        RgbaImage source = new(new ImageSize(2, 2), SourcePixels);

        using SkiaImage image = SkiaImageFactory.Create(source, "diagnostic");

        Assert.Equal(source.Size, image.Size);
        Assert.Equal("diagnostic", image.SourceName);
        Assert.Equal(SourcePixels[0], image.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(SourcePixels[3], image.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public void CreateProducesImageIndependentFromSourceArray()
    {
        Rgba32[] pixels = (Rgba32[])SourcePixels.Clone();
        RgbaImage source = new(new ImageSize(2, 2), pixels);
        using SkiaImage image = SkiaImageFactory.Create(source);

        pixels[0] = Rgba32.Opaque(0, 0, 0);

        Assert.Equal(SourcePixels[0], image.SampleNearest(new NormalizedPoint(0d, 0d)));
    }

    [Fact]
    public void CreateHonorsPreCancelledToken()
    {
        RgbaImage source = new(new ImageSize(2, 2), SourcePixels);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => SkiaImageFactory.Create(
            source,
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void CreateRejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() => SkiaImageFactory.Create(null!));
    }
}
