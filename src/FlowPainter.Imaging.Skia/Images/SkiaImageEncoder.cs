using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Images;

public sealed class SkiaImageEncoder
{
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The encoder is an application service with instance semantics so it can be injected, decorated or replaced without changing call sites.")]
    public async Task EncodeAsync(
        SkiaImage image,
        Stream output,
        RasterImageFormat format,
        int jpegQuality = 92,
        IProgress<ImageOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }

        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown raster image format.");
        }

        if (jpegQuality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jpegQuality),
                jpegQuality,
                "JPEG quality must be between 1 and 100.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        ImageOperationStage stage = format == RasterImageFormat.Png
            ? ImageOperationStage.EncodingPng
            : ImageOperationStage.EncodingJpeg;
        progress?.Report(new ImageOperationProgress(
            stage,
            0d,
            format == RasterImageFormat.Png ? "Encoding PNG" : "Encoding JPEG"));

        byte[] data = await Task.Run(
            () => Encode(image, format, jpegQuality),
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (output.CanSeek)
        {
            output.Position = 0L;
            output.SetLength(0L);
        }

        await output.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);

        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.Completed,
            1d,
            format == RasterImageFormat.Png ? "PNG encoded" : "JPEG encoded"));
    }

    private static byte[] Encode(
        SkiaImage source,
        RasterImageFormat format,
        int jpegQuality)
    {
        return format switch
        {
            RasterImageFormat.Png => EncodeSnapshot(source, SKEncodedImageFormat.Png, 100),
            RasterImageFormat.Jpeg => EncodeJpeg(source, jpegQuality),
            _ => throw new InvalidOperationException("Unknown raster image format.")
        };
    }

    private static byte[] EncodeSnapshot(
        SkiaImage source,
        SKEncodedImageFormat format,
        int quality)
    {
        using SKImage snapshot = source.CreateSnapshot();
        using SKData data = snapshot.Encode(format, quality)
            ?? throw new InvalidOperationException("SkiaSharp could not encode the image.");
        return data.ToArray();
    }

    private static byte[] EncodeJpeg(SkiaImage source, int quality)
    {
        SKImageInfo imageInfo = new(
            source.Size.Width,
            source.Size.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("SkiaSharp could not create the JPEG composition surface.");
        surface.Canvas.Clear(SKColors.White);
        using SKImage snapshot = source.CreateSnapshot();
        surface.Canvas.DrawImage(
            snapshot,
            new SKRect(0f, 0f, source.Size.Width, source.Size.Height),
            Sampling);
        using SKImage flattened = surface.Snapshot();
        using SKData data = flattened.Encode(SKEncodedImageFormat.Jpeg, quality)
            ?? throw new InvalidOperationException("SkiaSharp could not encode the image as JPEG.");
        return data.ToArray();
    }
}
