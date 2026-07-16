using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Images;

public static class FinalRenderMemoryEstimator
{
    public static FinalRenderMemoryEstimate Estimate(
        ImageSize sourceSize,
        ImageSize analysisProxySize,
        ImageSize previewSize,
        ImageSize outputSize,
        bool includeDetailOverlay = true)
    {
        return Estimate(
            sourceSize,
            analysisProxySize,
            previewSize,
            outputSize,
            GenerativeMode.FlowPainting,
            includeDetailOverlay);
    }

    public static FinalRenderMemoryEstimate Estimate(
        ImageSize sourceSize,
        ImageSize analysisProxySize,
        ImageSize previewSize,
        ImageSize outputSize,
        GenerativeMode mode,
        bool includeDetailOverlay = true)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown generative mode.");
        }

        long sourceBytes = sourceSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long analysisBytes = analysisProxySize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long previewBytes = previewSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long overlayBytes = includeDetailOverlay ? analysisBytes : 0L;
        long outputBytes = outputSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        AnalysisMemoryEstimate analysisEstimate = AnalysisMemoryEstimator.Estimate(
            sourceSize,
            analysisProxySize);
        long retainedOutputLayerBytes = mode == GenerativeMode.Hybrid
            ? checked(outputBytes * 2L)
            : 0L;
        long encodingReserveBytes = mode == GenerativeMode.Hybrid
            ? 0L
            : outputBytes;

        return new FinalRenderMemoryEstimate(
            sourceBytes,
            analysisBytes,
            previewBytes,
            overlayBytes,
            outputBytes,
            outputBytes)
        {
            Mode = mode,
            AnalysisWorkingBytes = analysisEstimate.CurrentAnalysisBytes,
            SegmentationReserveBytes = analysisEstimate.SegmentationReserveBytes,
            RetainedOutputLayerBytes = retainedOutputLayerBytes,
            EncodingReserveBytes = encodingReserveBytes
        };
    }
}
