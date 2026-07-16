using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Images;

public static class AnalysisMemoryEstimator
{
    public const int CurrentAnalysisBytesPerPixel = 160;

    public static AnalysisMemoryEstimate Estimate(
        ImageSize sourceSize,
        ImageSize proxySize)
    {
        long sourceBytes = sourceSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long proxyRgbaBytes = proxySize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long currentAnalysisBytes = checked(
            proxySize.PixelCount * CurrentAnalysisBytesPerPixel);
        RegionSegmentationEstimate segmentationEstimate = RegionSegmentationEstimator.Estimate(
            proxySize,
            new RegionSegmentationSettings());

        return new AnalysisMemoryEstimate(
            sourceBytes,
            proxyRgbaBytes,
            currentAnalysisBytes,
            segmentationEstimate.EstimatedPeakBytes);
    }
}
