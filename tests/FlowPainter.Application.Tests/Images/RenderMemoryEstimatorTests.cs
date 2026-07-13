using FlowPainter.Application.Images;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Images;

public sealed class RenderMemoryEstimatorTests
{
    [Fact]
    public void EstimateRgbaBuffersAccountsForAllExplicitBuffers()
    {
        RenderMemoryEstimate estimate = RenderMemoryEstimator.EstimateRgbaBuffers(
            new ImageSize(10_000, 10_000),
            new ImageSize(10_000, 10_000),
            new ImageSize(1_000, 1_000),
            new ImageSize(1_600, 1_600));

        Assert.Equal(400_000_000L, estimate.SourceBytes);
        Assert.Equal(400_000_000L, estimate.OutputBytes);
        Assert.Equal(4_000_000L, estimate.AnalysisProxyBytes);
        Assert.Equal(10_240_000L, estimate.PreviewBytes);
        Assert.Equal(814_240_000L, estimate.TotalRgbaBufferBytes);
    }
}
