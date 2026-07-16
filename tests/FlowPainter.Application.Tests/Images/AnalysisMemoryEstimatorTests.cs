using FlowPainter.Application.Images;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Images;

public sealed class AnalysisMemoryEstimatorTests
{
    [Fact]
    public void EstimateIncludesCurrentAnalysisAndFutureSegmentationReserve()
    {
        AnalysisMemoryEstimate estimate = AnalysisMemoryEstimator.Estimate(
            new ImageSize(1000, 500),
            new ImageSize(500, 250));

        Assert.Equal(2_000_000L, estimate.SourceBytes);
        Assert.Equal(500_000L, estimate.ProxyRgbaBytes);
        Assert.Equal(20_000_000L, estimate.CurrentAnalysisBytes);
        Assert.Equal(3_000_000L, estimate.SegmentationReserveBytes);
        Assert.Equal(25_500_000L, estimate.KnownPeakBytes);
    }
}
