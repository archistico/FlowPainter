using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Segmentation;

public sealed class RegionSegmentationRequest
{
    public RegionSegmentationRequest(
        IRgbaPixelSource source,
        RegionSegmentationSettings settings,
        long sourceRevision = 0,
        long settingsRevision = 0)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceRevision);
        ArgumentOutOfRangeException.ThrowIfNegative(settingsRevision);

        Source = source;
        Settings = settings;
        SourceRevision = sourceRevision;
        SettingsRevision = settingsRevision;
    }

    public IRgbaPixelSource Source { get; }

    public RegionSegmentationSettings Settings { get; }

    public long SourceRevision { get; }

    public long SettingsRevision { get; }
}
