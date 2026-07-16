using FlowPainter.Application.Images;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Images;

public sealed class FinalRenderMemoryEstimatorTests
{
    [Fact]
    public void EstimateAccountsForRenderingAnalysisAndSegmentationReserve()
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
        Assert.Equal(20_000_000L, estimate.AnalysisWorkingBytes);
        Assert.Equal(3_000_000L, estimate.SegmentationReserveBytes);
        Assert.Equal(32_000_000L, estimate.OutputSurfaceBytes);
        Assert.Equal(32_000_000L, estimate.OutputCopyBytes);
        Assert.Equal(32_000_000L, estimate.EncodingReserveBytes);
        Assert.Equal(96_000_000L, estimate.OutputWorkingBytes);
        Assert.Equal(122_500_000L, estimate.KnownPeakBytes);
        Assert.Equal(3, estimate.OutputBufferCount);
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
    public void HybridEstimateAccountsForFourOutputSizedBuffers()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(100, 100),
            new ImageSize(50, 50),
            new ImageSize(50, 50),
            new ImageSize(1000, 500),
            GenerativeMode.Hybrid);

        Assert.Equal(2_000_000L, estimate.OutputSurfaceBytes);
        Assert.Equal(4_000_000L, estimate.RetainedOutputLayerBytes);
        Assert.Equal(8_000_000L, estimate.OutputWorkingBytes);
        Assert.Equal(4, estimate.OutputBufferCount);
    }

    [Fact]
    public void PrimitiveEstimateUsesThreeOutputSizedBuffers()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(100, 100),
            new ImageSize(50, 50),
            new ImageSize(50, 50),
            new ImageSize(1000, 500),
            GenerativeMode.GeometricPrimitives);

        Assert.Equal(0L, estimate.RetainedOutputLayerBytes);
        Assert.Equal(2_000_000L, estimate.EncodingReserveBytes);
        Assert.Equal(6_000_000L, estimate.OutputWorkingBytes);
        Assert.Equal(3, estimate.OutputBufferCount);
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
    public void TenThousandPixelFlowOutputRemainsWithinProcessBudget()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(10_000, 10_000),
            new ImageSize(1024, 1024),
            new ImageSize(1024, 1024),
            new ImageSize(10_000, 10_000),
            GenerativeMode.FlowPainting);

        Assert.Equal(1_805_520_896L, estimate.KnownPeakBytes);
        Assert.Equal(FinalRenderMemoryRisk.High, estimate.Risk);
        Assert.True(WorkloadBudgetPolicy.IsMemoryWithinBudget(estimate.KnownPeakBytes));
    }

    [Fact]
    public void TenThousandPixelHybridOutputIsHighRiskAndBlocked()
    {
        FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
            new ImageSize(10_000, 10_000),
            new ImageSize(1024, 1024),
            new ImageSize(1024, 1024),
            new ImageSize(10_000, 10_000),
            GenerativeMode.Hybrid);

        Assert.Equal(2_205_520_896L, estimate.KnownPeakBytes);
        Assert.Equal(FinalRenderMemoryRisk.High, estimate.Risk);
        Assert.False(WorkloadBudgetPolicy.IsMemoryWithinBudget(estimate.KnownPeakBytes));
    }
}
