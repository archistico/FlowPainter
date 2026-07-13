using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Images;

public static class RenderMemoryEstimator
{
    public static RenderMemoryEstimate EstimateRgbaBuffers(
        ImageSize sourceSize,
        ImageSize outputSize,
        ImageSize analysisProxySize,
        ImageSize previewSize)
    {
        return new RenderMemoryEstimate(
            sourceSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel),
            outputSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel),
            analysisProxySize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel),
            previewSize.GetRequiredBytes(ImageSize.RgbaBytesPerPixel));
    }
}
