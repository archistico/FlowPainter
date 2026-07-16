using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionSegmentationProgressTests
{
    [Fact]
    public void ConstructorAcceptsValidProgress()
    {
        RegionSegmentationProgress progress = new(
            RegionSegmentationStage.UpdatingClusters,
            0.5d,
            0.7d,
            3,
            10);

        Assert.Equal(3, progress.CompletedIterations);
        Assert.Equal(0.7d, progress.OverallFraction);
    }

    [Fact]
    public void ConstructorRejectsUnknownStage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationProgress(
            (RegionSegmentationStage)99,
            0d,
            0d));
    }

    [Fact]
    public void ConstructorRejectsInvalidFractions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationProgress(
            RegionSegmentationStage.Preparing,
            -0.1d,
            0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationProgress(
            RegionSegmentationStage.Preparing,
            0d,
            double.NaN));
    }

    [Fact]
    public void ConstructorRejectsCompletedIterationsAboveTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationProgress(
            RegionSegmentationStage.UpdatingClusters,
            0d,
            0d,
            2,
            1));
    }
}
