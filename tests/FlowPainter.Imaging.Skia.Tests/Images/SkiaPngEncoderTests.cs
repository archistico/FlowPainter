using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Tests.Images;

public sealed class SkiaPngEncoderTests
{
    [Fact]
    public async Task EncodeAsyncProducesRoundTrippablePng()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Magenta);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        SkiaPngEncoder encoder = new();

        await encoder.EncodeAsync(source, output);
        output.Position = 0;
        using SkiaImage decoded = await new SkiaImageLoader().LoadAsync(output);

        Assert.Equal(Rgba32.Opaque(255, 0, 255), decoded.SampleNearest(new NormalizedPoint(0d, 0d)));
    }

    [Fact]
    public async Task EncodeAsyncHonorsPreCancelledToken()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaPngEncoder encoder = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => encoder.EncodeAsync(source, output, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task EncodeAsyncTruncatesSeekableOutputBeforeWriting()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Cyan);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new(new byte[16_384], writable: true);
        SkiaPngEncoder encoder = new();

        await encoder.EncodeAsync(source, output);

        Assert.True(output.Length < 16_384);
        output.Position = 0;
        using SkiaImage decoded = await new SkiaImageLoader().LoadAsync(output);
        Assert.Equal(Rgba32.Opaque(0, 255, 255), decoded.SampleNearest(new NormalizedPoint(0d, 0d)));
    }
}
