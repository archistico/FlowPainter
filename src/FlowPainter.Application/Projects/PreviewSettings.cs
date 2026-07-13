using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Projects;

public sealed class PreviewSettings
{
    public const int DraftMaximumDimension = 256;
    public const int StandardMaximumDimension = 512;
    public const int HighMaximumDimension = 1024;

    public PreviewSettings(PreviewQuality quality = PreviewQuality.Standard)
    {
        if (!Enum.IsDefined(quality))
        {
            throw new ArgumentOutOfRangeException(nameof(quality), quality, "Unknown preview quality.");
        }

        Quality = quality;
    }

    public PreviewQuality Quality { get; }

    public int MaximumDimension => Quality switch
    {
        PreviewQuality.Draft => DraftMaximumDimension,
        PreviewQuality.Standard => StandardMaximumDimension,
        PreviewQuality.High => HighMaximumDimension,
        _ => throw new InvalidOperationException("Unknown preview quality.")
    };

    public ImageSize Fit(ImageSize source)
    {
        return source.FitWithin(MaximumDimension, MaximumDimension);
    }
}
