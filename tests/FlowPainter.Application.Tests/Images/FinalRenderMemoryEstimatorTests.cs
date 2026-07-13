using FlowPainter.Application.Images;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Images;

public sealed class FinalRenderMemoryEstimatorTests
{
    [Fact]
    public void EstimateAccountsForTwoFinalOutputBuffers()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(1000, 500),
            new ImageSize(500, 250),
            new ImageSize(500, 250),
            new ImageSize(4000, 2000));

        Assert.Equal(2_000_000L, estimate.SourceBytes);
        Assert.Equal(500_000L, estimate.AnalysisProxyBytes);
        Assert.Equal(500_000L, estimate.PreviewBytes);
        Assert.Equal(500_000L, estimate.DetailOverlayBytes);
        Assert.Equal(32_000_000L, estimate.OutputSurfaceBytes);
        Assert.Equal(32_000_000L, estimate.OutputCopyBytes);
        Assert.Equal(64_000_000L, estimate.OutputWorkingBytes);
        Assert.Equal(67_500_000L, estimate.KnownPeakBytes);
    }

    [Fact]
    public void EstimateCanExcludeDetailOverlay()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(100, 100),
            new ImageSize(50, 50),
            new ImageSize(50, 50),
            new ImageSize(100, 100),
            includeDetailOverlay: false);

        Assert.Equal(0L, estimate.DetailOverlayBytes);
    }

    [Fact]
    public void RiskIsNormalBelowElevatedThreshold()
    {
        FinalRenderMemoryEstimate estimate = new(1, 1, 1, 1, 1, 1);

        Assert.Equal(FinalRenderMemoryRisk.Normal, estimate.Risk);
    }

    [Fact]
    public void RiskIsElevatedAtElevatedThreshold()
    {
        FinalRenderMemoryEstimate estimate = new(
            FinalRenderMemoryEstimate.ElevatedThresholdBytes,
            0,
            0,
            0,
            0,
            0);

        Assert.Equal(FinalRenderMemoryRisk.Elevated, estimate.Risk);
    }

    [Fact]
    public void RiskIsHighAtHighThreshold()
    {
        FinalRenderMemoryEstimate estimate = new(
            FinalRenderMemoryEstimate.HighThresholdBytes,
            0,
            0,
            0,
            0,
            0);

        Assert.Equal(FinalRenderMemoryRisk.High, estimate.Risk);
    }

    [Fact]
    public void TenThousandPixelSourceAndOutputHaveElevatedKnownPeak()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(10_000, 10_000),
            new ImageSize(1024, 1024),
            new ImageSize(1024, 1024),
            new ImageSize(10_000, 10_000));

        Assert.Equal(1_212_582_912L, estimate.KnownPeakBytes);
        Assert.Equal(FinalRenderMemoryRisk.Elevated, estimate.Risk);
    }
}
