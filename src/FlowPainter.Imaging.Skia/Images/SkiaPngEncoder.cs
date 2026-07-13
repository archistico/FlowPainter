using System.Diagnostics.CodeAnalysis;

namespace FlowPainter.Imaging.Skia.Images;

public sealed class SkiaPngEncoder
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The encoder is an application service with instance semantics so it can be injected, decorated or replaced without changing call sites.")]
    public async Task EncodeAsync(
        SkiaImage image,
        Stream output,
        IProgress<ImageOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.EncodingPng,
            0d,
            "Encoding PNG"));

        byte[] data = await Task.Run(image.EncodePng, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (output.CanSeek)
        {
            output.Position = 0;
            output.SetLength(0);
        }

        await output.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);

        progress?.Report(new ImageOperationProgress(
            ImageOperationStage.Completed,
            1d,
            "PNG encoded"));
    }
}
