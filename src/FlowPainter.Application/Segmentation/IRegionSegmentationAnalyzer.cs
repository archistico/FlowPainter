namespace FlowPainter.Application.Segmentation;

public interface IRegionSegmentationAnalyzer
{
    Task<RegionSegmentationResult> AnalyzeAsync(
        RegionSegmentationRequest request,
        IProgress<RegionSegmentationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
