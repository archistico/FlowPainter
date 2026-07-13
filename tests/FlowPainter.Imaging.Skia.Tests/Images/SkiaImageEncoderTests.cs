using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Tests.Images;

public sealed class SkiaImageEncoderTests
{
    [Theory]
    [InlineData(RasterImageFormat.Png)]
    [InlineData(RasterImageFormat.Jpeg)]
    public async Task EncodeAsyncProducesDecodableImage(RasterImageFormat format)
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(8, 8, (_, _) => SKColors.Magenta);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        SkiaImageEncoder encoder = new();

        await encoder.EncodeAsync(source, output, format);
        output.Position = 0L;
        using SkiaImage decoded = await new SkiaImageLoader().LoadAsync(output);

        Rgba32 pixel = decoded.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        Assert.True(pixel.Red > 240);
        Assert.True(pixel.Blue > 240);
        Assert.True(pixel.Green < 20);
    }

    [Fact]
    public async Task EncodeJpegFlattensTransparencyAgainstWhite()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(8, 8, (_, _) => SKColors.Transparent);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        SkiaImageEncoder encoder = new();

        await encoder.EncodeAsync(source, output, RasterImageFormat.Jpeg, jpegQuality: 100);
        output.Position = 0L;
        using SkiaImage decoded = await new SkiaImageLoader().LoadAsync(output);

        Rgba32 pixel = decoded.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        Assert.True(pixel.Red > 245);
        Assert.True(pixel.Green > 245);
        Assert.True(pixel.Blue > 245);
        Assert.Equal((byte)255, pixel.Alpha);
    }

    [Fact]
    public async Task EncodeAsyncTruncatesSeekableOutput()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(2, 2, (_, _) => SKColors.Cyan);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new(new byte[32_768], writable: true);
        SkiaImageEncoder encoder = new();

        await encoder.EncodeAsync(source, output, RasterImageFormat.Png);

        Assert.True(output.Length < 32_768L);
    }

    [Fact]
    public async Task EncodeAsyncRejectsReadOnlyOutput()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new([], writable: false);
        SkiaImageEncoder encoder = new();

        await Assert.ThrowsAsync<ArgumentException>(() => encoder.EncodeAsync(
            source,
            output,
            RasterImageFormat.Png));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task EncodeAsyncRejectsInvalidJpegQuality(int quality)
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        SkiaImageEncoder encoder = new();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => encoder.EncodeAsync(
            source,
            output,
            RasterImageFormat.Jpeg,
            quality));
    }

    [Fact]
    public async Task EncodeAsyncRejectsUnknownFormat()
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        SkiaImageEncoder encoder = new();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => encoder.EncodeAsync(
            source,
            output,
            (RasterImageFormat)99));
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
        SkiaImageEncoder encoder = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => encoder.EncodeAsync(
            source,
            output,
            RasterImageFormat.Png,
            cancellationToken: cancellation.Token));
    }

    [Theory]
    [InlineData(RasterImageFormat.Png, ImageOperationStage.EncodingPng)]
    [InlineData(RasterImageFormat.Jpeg, ImageOperationStage.EncodingJpeg)]
    public async Task EncodeAsyncReportsFormatSpecificProgress(
        RasterImageFormat format,
        ImageOperationStage expectedStage)
    {
        byte[] sourcePng = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream input = new(sourcePng, writable: false);
        using SkiaImage source = await new SkiaImageLoader().LoadAsync(input);
        using MemoryStream output = new();
        RecordingProgress<ImageOperationProgress> progress = new();
        SkiaImageEncoder encoder = new();

        await encoder.EncodeAsync(source, output, format, progress: progress);

        Assert.Equal(expectedStage, progress.Values[0].Stage);
        Assert.Equal(ImageOperationStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }
}
