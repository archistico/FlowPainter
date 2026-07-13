using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Projects;

public sealed class FinalRenderSettings
{
    public const int DefaultMaximumDimension = 4096;
    public const int DefaultJpegQuality = 92;

    public FinalRenderSettings(
        int maximumDimension = DefaultMaximumDimension,
        RasterImageFormat format = RasterImageFormat.Png,
        int jpegQuality = DefaultJpegQuality)
    {
        if (maximumDimension <= 0 || maximumDimension > ImageSize.MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumDimension),
                maximumDimension,
                $"Final output dimension must be between 1 and {ImageSize.MaximumDimension:N0} pixels.");
        }

        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown final image format.");
        }

        if (jpegQuality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jpegQuality),
                jpegQuality,
                "JPEG quality must be between 1 and 100.");
        }

        MaximumDimension = maximumDimension;
        Format = format;
        JpegQuality = jpegQuality;
    }

    public int MaximumDimension { get; }

    public RasterImageFormat Format { get; }

    public int JpegQuality { get; }

    public ImageSize GetOutputSize(ImageSize sourceSize)
    {
        return sourceSize.FitWithin(
            MaximumDimension,
            MaximumDimension,
            allowUpscale: true);
    }

    public string DefaultFileExtension => Format switch
    {
        RasterImageFormat.Png => "png",
        RasterImageFormat.Jpeg => "jpg",
        _ => throw new InvalidOperationException("Unknown final image format.")
    };
}
