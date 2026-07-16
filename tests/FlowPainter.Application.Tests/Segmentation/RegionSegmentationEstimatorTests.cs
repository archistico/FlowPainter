using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionSegmentationEstimatorTests
{
    [Fact]
    public void EstimateCalculatesRegionsMemoryAndWork()
    {
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(
            new ImageSize(128, 64),
            new RegionSegmentationSettings());

        Assert.Equal(2, estimate.EstimatedRegionCount);
        Assert.Equal(RegionLabelStorageKind.Compact, estimate.LabelStorageKind);
        Assert.Equal(16_384L, estimate.LabelBytes);
        Assert.Equal(213_184L, estimate.EstimatedPeakBytes);
        Assert.Equal(737_280L, estimate.EstimatedAssignmentEvaluations);
    }

    [Fact]
    public void EstimateOmitsSmoothingBufferWhenDisabled()
    {
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(
            new ImageSize(128, 64),
            new RegionSegmentationSettings(preBlurSigma: 0d));

        Assert.Equal(0L, estimate.SmoothingBufferBytes);
        Assert.Equal(180_416L, estimate.EstimatedPeakBytes);
    }

    [Fact]
    public void EstimateSelectsUInt32ForVeryFineLargeProxy()
    {
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(
            new ImageSize(10_000, 10_000),
            new RegionSegmentationSettings(targetRegionSize: 4));

        Assert.Equal(6_250_000, estimate.EstimatedRegionCount);
        Assert.Equal(RegionLabelStorageKind.Wide, estimate.LabelStorageKind);
        Assert.Equal(400_000_000L, estimate.LabelBytes);
    }

    [Fact]
    public void EstimateRoundsClusterGridUpAtImageEdges()
    {
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(
            new ImageSize(65, 65),
            new RegionSegmentationSettings(targetRegionSize: 64));

        Assert.Equal(4, estimate.EstimatedRegionCount);
    }

    [Fact]
    public void EstimateWorkScalesWithMaximumIterations()
    {
        ImageSize size = new(100, 100);
        RegionSegmentationEstimate low = RegionSegmentationEstimator.Estimate(
            size,
            new RegionSegmentationSettings(maximumIterations: 5));
        RegionSegmentationEstimate high = RegionSegmentationEstimator.Estimate(
            size,
            new RegionSegmentationSettings(maximumIterations: 10));

        Assert.Equal(low.EstimatedAssignmentEvaluations * 2, high.EstimatedAssignmentEvaluations);
    }
}
