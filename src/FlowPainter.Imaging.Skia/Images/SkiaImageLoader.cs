using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Images;

public sealed class SkiaImageLoader
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The loader is an application service with instance semantics so it can be injected, decorated or replaced without changing call sites.")]
    public async Task<SkiaImage> LoadAsync(
        Stream input,
        string? sourceName = null,
        IProgress<ImageOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead)
        {
            throw new ArgumentException("The input stream must be readable.", nameof(input));
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.ReadingEncodedData,
            0d,
            "Reading encoded image data"));

        using MemoryStream encodedBuffer = new();
        await input.CopyToAsync(encodedBuffer, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] encodedData = encodedBuffer.ToArray();
        return await Task.Run(
            () => Decode(encodedData, sourceName, progress, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The decoded SKBitmap ownership is transferred to the returned SkiaImage; every failure path disposes that wrapper.")]
    private static SkiaImage Decode(
        byte[] encodedData,
        string? sourceName,
        IProgress<ImageOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (encodedData.Length == 0)
        {
            throw new InvalidDataException("The image stream is empty.");
        }

        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.InspectingMetadata,
            0.25d,
            "Inspecting image metadata"));

        using SKData data = SKData.CreateCopy(encodedData);
        using SKCodec codec = SKCodec.Create(data)
            ?? throw new InvalidDataException("The selected file is not a supported image.");

        int width = codec.Info.Width;
        int height = codec.Info.Height;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("The image reports invalid dimensions.");
        }

        if (width > ImageSize.MaximumDimension || height > ImageSize.MaximumDimension)
        {
            throw new UnsupportedImageDimensionsException(width, height, ImageSize.MaximumDimension);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.DecodingPixels,
            0.5d,
            "Decoding image pixels"));

        SKImageInfo targetInfo = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        SKBitmap bitmap = SKBitmap.Decode(codec, targetInfo)
            ?? throw new InvalidDataException("SkiaSharp could not decode the selected image.");
        SkiaImage result = new(bitmap, sourceName);
        bool completed = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ImageOperationProgress(
                ImageOperationStage.Completed,
                1d,
                "Image loaded"));

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
