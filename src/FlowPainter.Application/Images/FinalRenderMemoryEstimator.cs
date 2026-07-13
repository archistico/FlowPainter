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
        long sourceBytes = sourceSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long analysisBytes = analysisProxySize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long previewBytes = previewSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long overlayBytes = includeDetailOverlay ? analysisBytes : 0L;
        long outputBytes = outputSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);

        return new FinalRenderMemoryEstimate(
            sourceBytes,
            analysisBytes,
            previewBytes,
            overlayBytes,
            outputBytes,
            outputBytes);
    }
}
