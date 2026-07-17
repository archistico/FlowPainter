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
        Assert.Equal(32_768L, estimate.DescriptorBufferBytes);
        Assert.Equal(640L, estimate.DescriptorRegionBytes);
        Assert.Equal(2_048L, estimate.AdjacencyRegionBytes);
        Assert.Equal(4_096L, estimate.HierarchyRegionBytes);
        Assert.Equal(318_592L, estimate.EstimatedPeakBytes);
        Assert.Equal(737_280L, estimate.EstimatedAssignmentEvaluations);
    }

    [Fact]
    public void EstimateDisabledSegmentationUsesSingleRegionWithoutClusteringWork()
    {
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(
            new ImageSize(128, 64),
            new RegionSegmentationSettings(enabled: false));

        Assert.Equal(1, estimate.EstimatedRegionCount);
        Assert.Equal(RegionLabelStorageKind.Compact, estimate.LabelStorageKind);
        Assert.Equal(0L, estimate.ColorBufferBytes);
        Assert.Equal(0L, estimate.DistanceBufferBytes);
        Assert.Equal(0L, estimate.AssignmentBufferBytes);
        Assert.Equal(0L, estimate.SmoothingBufferBytes);
        Assert.Equal(0L, estimate.ConnectivityBufferBytes);
        Assert.Equal(0L, estimate.EstimatedAssignmentEvaluations);
    }

    [Fact]
    public void EstimateOmitsSmoothingBufferWhenDisabled()
    {
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(
            new ImageSize(128, 64),
            new RegionSegmentationSettings(preBlurSigma: 0d));

        Assert.Equal(0L, estimate.SmoothingBufferBytes);
        Assert.Equal(285_824L, estimate.EstimatedPeakBytes);
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
