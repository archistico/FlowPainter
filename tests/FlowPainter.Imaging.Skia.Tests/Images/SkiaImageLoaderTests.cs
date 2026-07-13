using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Tests.Images;

public sealed class SkiaImageLoaderTests
{
    [Fact]
    public async Task LoadAsyncDecodesDimensionsPixelsAndSourceName()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(
            2,
            2,
            (x, y) => (x, y) switch
            {
                (0, 0) => SKColors.Red,
                (1, 0) => SKColors.Green,
                (0, 1) => SKColors.Blue,
                _ => SKColors.White
            });
        using MemoryStream stream = new(png, writable: false);
        SkiaImageLoader loader = new();

        using SkiaImage image = await loader.LoadAsync(stream, "  fixture.png  ");

        Assert.Equal(new ImageSize(2, 2), image.Size);
        Assert.Equal("fixture.png", image.SourceName);
        Assert.Equal(Rgba32.Opaque(255, 0, 0), image.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(Rgba32.Opaque(255, 255, 255), image.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public async Task LoadAsyncReportsOrderedStages()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream stream = new(png, writable: false);
        RecordingProgress<ImageOperationProgress> progress = new();
        SkiaImageLoader loader = new();

        using SkiaImage image = await loader.LoadAsync(stream, progress: progress);

        ImageOperationStage[] expectedStages =
        [
            ImageOperationStage.ReadingEncodedData,
            ImageOperationStage.InspectingMetadata,
            ImageOperationStage.DecodingPixels,
            ImageOperationStage.Completed
        ];

        Assert.Equal(expectedStages, progress.Values.Select(value => value.Stage));
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public async Task LoadAsyncRejectsUnsupportedImageDimensions()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(
            ImageSize.MaximumDimension + 1,
            1,
            (_, _) => SKColors.Black);
        using MemoryStream stream = new(png, writable: false);
        SkiaImageLoader loader = new();

        UnsupportedImageDimensionsException exception = await Assert.ThrowsAsync<UnsupportedImageDimensionsException>(
            () => loader.LoadAsync(stream));

        Assert.Equal(ImageSize.MaximumDimension + 1, exception.Width);
        Assert.Equal(1, exception.Height);
    }

    [Fact]
    public async Task LoadAsyncRejectsInvalidEncodedData()
    {
        using MemoryStream stream = new(new byte[] { 1, 2, 3, 4 }, writable: false);
        SkiaImageLoader loader = new();

        await Assert.ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(stream));
    }

    [Fact]
    public async Task LoadAsyncHonorsPreCancelledToken()
    {
        using MemoryStream stream = new(new byte[] { 1, 2, 3, 4 }, writable: false);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaImageLoader loader = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => loader.LoadAsync(stream, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task DisposedImageRejectsSamplingAndEncoding()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream stream = new(png, writable: false);
        SkiaImageLoader loader = new();
        SkiaImage image = await loader.LoadAsync(stream);

        image.Dispose();

        Assert.True(image.IsDisposed);
        Assert.Throws<ObjectDisposedException>(
            () => image.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Throws<ObjectDisposedException>(() => image.EncodePng());
    }
}
