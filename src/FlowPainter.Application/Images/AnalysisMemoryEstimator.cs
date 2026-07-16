using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Images;

public static class AnalysisMemoryEstimator
{
    public const int CurrentAnalysisBytesPerPixel = 160;
    public const int SegmentationReserveBytesPerPixel = 24;

    public static AnalysisMemoryEstimate Estimate(
        ImageSize sourceSize,
        ImageSize proxySize)
    {
        long sourceBytes = sourceSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long proxyRgbaBytes = proxySize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long currentAnalysisBytes = checked(
            proxySize.PixelCount * CurrentAnalysisBytesPerPixel);
        long segmentationReserveBytes = checked(
            proxySize.PixelCount * SegmentationReserveBytesPerPixel);

        return new AnalysisMemoryEstimate(
            sourceBytes,
            proxyRgbaBytes,
            currentAnalysisBytes,
            segmentationReserveBytes);
    }
}
