using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Images;

public sealed class SkiaImageProxyGenerator
{
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The proxy generator is an application service with instance semantics so it can be injected, decorated or replaced without changing call sites.")]
    public Task<SkiaImage> CreateProxyAsync(
        SkiaImage source,
        int maximumWidth,
        int maximumHeight,
        IProgress<ImageOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ImageSize targetSize = source.Size.FitWithin(maximumWidth, maximumHeight);

        return Task.Run(
            () => CreateProxy(source, targetSize, progress, cancellationToken),
            cancellationToken);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The copied SKBitmap ownership is transferred to the returned SkiaImage; every failure path disposes that wrapper.")]
    private static SkiaImage CreateProxy(
        SkiaImage source,
        ImageSize targetSize,
        IProgress<ImageOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.CreatingProxy,
            0d,
            "Creating analysis proxy"));

        SKImageInfo targetInfo = new(
            targetSize.Width,
            targetSize.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using SKSurface surface = SKSurface.Create(targetInfo)
            ?? throw new InvalidOperationException("SkiaSharp could not create the proxy surface.");
        using SKImage sourceImage = source.CreateSnapshot();

        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(
            sourceImage,
            new SKRect(0f, 0f, targetSize.Width, targetSize.Height),
            Sampling);

        cancellationToken.ThrowIfCancellationRequested();
        using SKImage snapshot = surface.Snapshot();
        SKBitmap bitmap = SKBitmap.FromImage(snapshot)
            ?? throw new InvalidOperationException("SkiaSharp could not copy the proxy pixels.");
        SkiaImage result = new(bitmap, source.SourceName);
        bool completed = false;

        try
        {
            progress?.Report(new ImageOperationProgress(
                ImageOperationStage.Completed,
                1d,
                "Analysis proxy created"));

            completed = true;
            return result;
        }
        finally
        {
            if (!completed)
            {
                result.Dispose();
            }
        }
    }
}
