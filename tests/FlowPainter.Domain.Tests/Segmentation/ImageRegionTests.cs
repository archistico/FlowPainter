using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Domain.Tests.Segmentation;

public sealed class ImageRegionTests
{
    [Fact]
    public void ConstructorAcceptsValidCoreAndDescriptors()
    {
        RegionVisualDescriptors descriptors = new(
            perimeter: 12d,
            compactness: 0.8d,
            meanLightness: 50d,
            edgeDensity: 0.25d);

        ImageRegion region = new(
            0,
            4,
            0.5d,
            new PixelBounds(0, 0, 2, 2),
            new RegionCentroid(0.5d, 0.5d),
            descriptors);

        Assert.Same(descriptors, region.Descriptors);
        Assert.Equal(4, region.PixelCount);
    }

    [Fact]
    public void ConstructorUsesEmptyDescriptorsByDefault()
    {
        ImageRegion region = new(
            0,
            1,
            1d,
            new PixelBounds(0, 0, 1, 1),
            new RegionCentroid(0d, 0d));

        Assert.Same(RegionVisualDescriptors.Empty, region.Descriptors);
    }

    [Fact]
    public void ConstructorRejectsCentroidOutsideBounds()
    {
        Assert.Throws<ArgumentException>(() => new ImageRegion(
            0,
            4,
            1d,
            new PixelBounds(0, 0, 2, 2),
            new RegionCentroid(2d, 1d)));
    }

    [Fact]
    public void VisualDescriptorsRejectInvalidRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionVisualDescriptors(compactness: 1.1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionVisualDescriptors(meanLightness: 101d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionVisualDescriptors(edgeDensity: -0.1d));
    }

    [Fact]
    public void PixelBoundsUseExclusiveRightAndBottomEdges()
    {
        PixelBounds bounds = new(1, 2, 4, 6);

        Assert.True(bounds.Contains(1, 2));
        Assert.True(bounds.Contains(3, 5));
        Assert.False(bounds.Contains(4, 5));
        Assert.False(bounds.Contains(3, 6));
    }
}
