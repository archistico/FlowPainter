using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Boundaries;
using FlowPainter.Rendering.Skia.Tests.Strokes;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Tests.Boundaries;

public sealed class BoundaryDirectionOverlayRendererTests
{
    [Fact]
    public async Task RenderAsyncPreservesSourceDimensions()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            24,
            18,
            (_, _) => new SKColor(100, 100, 100, 255));
        BoundaryDirectionField field = CreateUniformField(source.Size, new BoundaryVector(1d, 0d));
        DetailMap importance = DetailMap.CreateUniform(source.Size, 1f);

        using SkiaImage result = await new BoundaryDirectionOverlayRenderer().RenderAsync(
            source,
            field,
            importance,
            gridSpacing: 6);

        Assert.Equal(source.Size, result.Size);
    }

    [Fact]
    public async Task RenderAsyncDrawsDirectionForImportantCells()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            24,
            18,
            (_, _) => new SKColor(40, 40, 40, 255));
        BoundaryDirectionField field = CreateUniformField(source.Size, new BoundaryVector(1d, 0d));
        DetailMap importance = DetailMap.CreateUniform(source.Size, 1f);

        using SkiaImage result = await new BoundaryDirectionOverlayRenderer().RenderAsync(
            source,
            field,
            importance,
            gridSpacing: 6,
            opacity: 1d);

        FlowPainter.Domain.Color.Rgba32 original = source.SampleNearest(new NormalizedPoint(0.375d, 0.5d));
        FlowPainter.Domain.Color.Rgba32 overlay = result.SampleNearest(new NormalizedPoint(0.375d, 0.5d));
        Assert.NotEqual(original, overlay);
        Assert.True(overlay.Blue > original.Blue);
    }

    [Fact]
    public async Task RenderAsyncLeavesSourceUnchangedBelowImportanceThreshold()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            24,
            18,
            (_, _) => new SKColor(40, 40, 40, 255));
        BoundaryDirectionField field = CreateUniformField(source.Size, new BoundaryVector(1d, 0d));
        DetailMap importance = DetailMap.CreateUniform(source.Size, 0f);

        using SkiaImage result = await new BoundaryDirectionOverlayRenderer().RenderAsync(
            source,
            field,
            importance,
            gridSpacing: 6,
            minimumImportance: 0.5d,
            opacity: 1d);

        Assert.Equal(
            source.SampleNearest(new NormalizedPoint(0.375d, 0.5d)),
            result.SampleNearest(new NormalizedPoint(0.375d, 0.5d)));
    }

    [Fact]
    public async Task RenderAsyncRejectsMismatchedDimensions()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            4,
            4,
            (_, _) => SKColors.Black);
        BoundaryDirectionField field = BoundaryDirectionField.CreateEmpty(new ImageSize(2, 2));
        DetailMap importance = DetailMap.CreateUniform(source.Size, 1f);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new BoundaryDirectionOverlayRenderer().RenderAsync(source, field, importance));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(129)]
    public async Task RenderAsyncRejectsInvalidGridSpacing(int spacing)
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            4,
            4,
            (_, _) => SKColors.Black);
        BoundaryDirectionField field = BoundaryDirectionField.CreateEmpty(source.Size);
        DetailMap importance = DetailMap.CreateUniform(source.Size, 1f);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new BoundaryDirectionOverlayRenderer().RenderAsync(
                source,
                field,
                importance,
                gridSpacing: spacing));
    }

    [Fact]
    public async Task RenderAsyncHonorsPreCancelledToken()
    {
        using SkiaImage source = await RendererTestImageFactory.LoadAsync(
            4,
            4,
            (_, _) => SKColors.Black);
        BoundaryDirectionField field = BoundaryDirectionField.CreateEmpty(source.Size);
        DetailMap importance = DetailMap.CreateUniform(source.Size, 1f);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new BoundaryDirectionOverlayRenderer().RenderAsync(
                source,
                field,
                importance,
                cancellationToken: cancellation.Token));
    }

    private static BoundaryDirectionField CreateUniformField(
        ImageSize size,
        BoundaryVector vector)
    {
        BoundaryVector[] vectors = new BoundaryVector[checked((int)size.PixelCount)];
        Array.Fill(vectors, vector);
        return new BoundaryDirectionField(size.Width, size.Height, vectors);
    }
}
