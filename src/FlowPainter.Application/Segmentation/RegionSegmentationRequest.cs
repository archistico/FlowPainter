using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Segmentation;

public sealed class RegionSegmentationRequest
{
    public RegionSegmentationRequest(
        IRgbaPixelSource source,
        RegionSegmentationSettings settings,
        long sourceRevision = 0,
        long settingsRevision = 0,
        RegionMergeSettings? mergeSettings = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceRevision);
        ArgumentOutOfRangeException.ThrowIfNegative(settingsRevision);

        Source = source;
        Settings = settings;
        MergeSettings = mergeSettings ?? new RegionMergeSettings();
        SourceRevision = sourceRevision;
        SettingsRevision = settingsRevision;
    }

    public IRgbaPixelSource Source { get; }

    public RegionSegmentationSettings Settings { get; }

    public RegionMergeSettings MergeSettings { get; }

    public long SourceRevision { get; }

    public long SettingsRevision { get; }
}
