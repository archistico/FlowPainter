using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Tests.Geometry;

public sealed class NormalizedRectTests
{
    [Fact]
    public void FromCornersNormalizesReverseMouseDrag()
    {
        NormalizedRect region = NormalizedRect.FromCorners(
            new NormalizedPoint(0.8d, 0.7d),
            new NormalizedPoint(0.2d, 0.1d));

        Assert.Equal(0.2d, region.Left, 12);
        Assert.Equal(0.1d, region.Top, 12);
        Assert.Equal(0.8d, region.Right, 12);
        Assert.Equal(0.7d, region.Bottom, 12);
    }

    [Fact]
    public void ContainsIncludesRegionEdges()
    {
        NormalizedRect region = new(0.2d, 0.2d, 0.8d, 0.8d);

        Assert.True(region.Contains(new NormalizedPoint(0.2d, 0.2d)));
        Assert.True(region.Contains(new NormalizedPoint(0.8d, 0.8d)));
        Assert.False(region.Contains(new NormalizedPoint(0.1d, 0.5d)));
    }

    [Fact]
    public void ConstructorRejectsZeroAreaRegion()
    {
        Assert.Throws<ArgumentException>(() => new NormalizedRect(0.5d, 0.2d, 0.5d, 0.8d));
    }
}
