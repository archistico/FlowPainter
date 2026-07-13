using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Detail;
using FlowPainter.Rendering.Skia.Tests.Strokes;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Tests.Detail;

public sealed class DetailMapOverlayRendererTests
{
    [Fact]
    public async Task RenderAsyncPreservesSourceDimensions()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            3,
            2,
            (_, _) => new SKColor(100, 100, 100, 255));
        DetailMap map = DetailMap.CreateUniform(source.Size, 0.5f);

        using SkiaImage result = await new DetailMapOverlayRenderer().RenderAsync(source, map);

        Assert.Equal(source.Size, result.Size);
    }

    [Fact]
    public async Task RenderAsyncColorsLowAndHighDetailDifferently()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            2,
            1,
            (_, _) => new SKColor(128, 128, 128, 255));
        DetailMap map = new(2, 1, [0f, 1f]);

        using SkiaImage result = await new DetailMapOverlayRenderer().RenderAsync(
            source,
            map,
            opacity: 1d);

        FlowPainter.Domain.Color.Rgba32 low = result.SampleNearest(new NormalizedPoint(0.25d, 0.5d));
        FlowPainter.Domain.Color.Rgba32 high = result.SampleNearest(new NormalizedPoint(0.75d, 0.5d));
        Assert.True(low.Blue > low.Red);
        Assert.True(high.Red > high.Blue);
    }

    [Fact]
    public async Task RenderAsyncWithZeroOpacityPreservesPixels()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            1,
            1,
            (_, _) => new SKColor(12, 34, 56, 200));
        DetailMap map = DetailMap.CreateUniform(source.Size, 1f);

        using SkiaImage result = await new DetailMapOverlayRenderer().RenderAsync(
            source,
            map,
            opacity: 0d);

        Assert.Equal(
            source.SampleNearest(new NormalizedPoint(0.5d, 0.5d)),
            result.SampleNearest(new NormalizedPoint(0.5d, 0.5d)));
    }

    [Fact]
    public async Task RenderAsyncRejectsMismatchedDimensions()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            2,
            2,
            (_, _) => SKColors.Black);
        DetailMap map = DetailMap.CreateUniform(new ImageSize(1, 1), 0.5f);

        await Assert.ThrowsAsync<ArgumentException>(
            () => new DetailMapOverlayRenderer().RenderAsync(source, map));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public async Task RenderAsyncRejectsInvalidOpacity(double value)
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            1,
            1,
            (_, _) => SKColors.Black);
        DetailMap map = DetailMap.CreateUniform(source.Size, 0.5f);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new DetailMapOverlayRenderer().RenderAsync(source, map, value));
    }

    [Fact]
    public async Task RenderAsyncHonorsPreCancelledToken()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            1,
            1,
            (_, _) => SKColors.Black);
        DetailMap map = DetailMap.CreateUniform(source.Size, 0.5f);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new DetailMapOverlayRenderer().RenderAsync(
                source,
                map,
                cancellationToken: cancellation.Token));
    }
}
