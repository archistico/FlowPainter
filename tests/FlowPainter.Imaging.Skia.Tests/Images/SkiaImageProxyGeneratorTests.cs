using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Tests.Images;

public sealed class SkiaImageProxyGeneratorTests
{
    [Fact]
    public async Task CreateProxyAsyncPreservesAspectRatioWithoutUpscaling()
    {
        using SkiaImage source = await LoadAsync(4, 2);
        SkiaImageProxyGenerator generator = new();

        using SkiaImage proxy = await generator.CreateProxyAsync(source, 2, 2);

        Assert.Equal(new ImageSize(2, 1), proxy.Size);
    }

    [Fact]
    public async Task CreateProxyAsyncReturnsIndependentCopyWhenSourceAlreadyFits()
    {
        using SkiaImage source = await LoadAsync(2, 1);
        SkiaImageProxyGenerator generator = new();
        using SkiaImage proxy = await generator.CreateProxyAsync(source, 4, 4);

        source.Dispose();

        Assert.Equal(new ImageSize(2, 1), proxy.Size);
        Assert.Equal(Rgba32.Opaque(255, 0, 0), proxy.SampleNearest(new NormalizedPoint(0d, 0d)));
    }

    [Fact]
    public async Task CreateProxyAsyncReportsCompletion()
    {
        using SkiaImage source = await LoadAsync(2, 1);
        RecordingProgress<ImageOperationProgress> progress = new();
        SkiaImageProxyGenerator generator = new();

        using SkiaImage proxy = await generator.CreateProxyAsync(source, 1, 1, progress);

        Assert.Equal(ImageOperationStage.CreatingProxy, progress.Values[0].Stage);
        Assert.Equal(ImageOperationStage.Completed, progress.Values[^1].Stage);
    }

    [Fact]
    public async Task CreateProxyAsyncHonorsPreCancelledToken()
    {
        using SkiaImage source = await LoadAsync(2, 1);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaImageProxyGenerator generator = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.CreateProxyAsync(source, 1, 1, cancellationToken: cancellation.Token));
    }

    private static async Task<SkiaImage> LoadAsync(int width, int height)
    {
        byte[] png = SkiaTestImageFactory.CreatePng(
            width,
            height,
            (x, _) => x == 0 ? SKColors.Red : SKColors.Blue);
        using MemoryStream stream = new(png, writable: false);
        return await new SkiaImageLoader().LoadAsync(stream);
    }
}
