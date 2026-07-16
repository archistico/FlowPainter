using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Images;

public sealed class SkiaImageLoader
{
    public const int DefaultMaximumEncodedImageBytes = 256 * 1024 * 1024;
    private const int CopyBufferSize = 81_920;
    private readonly int _maximumEncodedImageBytes;

    public SkiaImageLoader(int maximumEncodedImageBytes = DefaultMaximumEncodedImageBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEncodedImageBytes, 1);
        _maximumEncodedImageBytes = maximumEncodedImageBytes;
    }

    public int MaximumEncodedImageBytes => _maximumEncodedImageBytes;

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

        byte[] encodedData = await ReadEncodedDataAsync(input, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(
            () => Decode(encodedData, sourceName, progress, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> ReadEncodedDataAsync(
        Stream input,
        CancellationToken cancellationToken)
    {
        if (input.CanSeek)
        {
            long remaining = input.Length - input.Position;
            ValidateEncodedLength(remaining);
            byte[] encodedData = new byte[checked((int)remaining)];
            await input.ReadExactlyAsync(encodedData, cancellationToken).ConfigureAwait(false);
            return encodedData;
        }

        using MemoryStream encodedBuffer = new(Math.Min(CopyBufferSize, _maximumEncodedImageBytes));
        byte[] copyBuffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            int totalBytes = 0;
            while (true)
            {
                int read = await input.ReadAsync(copyBuffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                totalBytes = checked(totalBytes + read);
                ValidateEncodedLength(totalBytes);
                await encodedBuffer.WriteAsync(
                    copyBuffer.AsMemory(0, read),
                    cancellationToken).ConfigureAwait(false);
            }

            return encodedBuffer.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(copyBuffer);
        }
    }

    private void ValidateEncodedLength(long encodedLength)
    {
        if (encodedLength < 0)
        {
            throw new InvalidDataException("The image stream reports an invalid encoded length.");
        }

        if (encodedLength > _maximumEncodedImageBytes)
        {
            double maximumMebibytes = _maximumEncodedImageBytes / 1024d / 1024d;
            throw new InvalidDataException(
                $"The encoded image exceeds the supported {maximumMebibytes:N0} MiB input limit.");
        }
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
