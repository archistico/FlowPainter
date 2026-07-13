using FlowPainter.Application.Interaction;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Interaction;

public sealed class UniformImageViewportTests
{
    [Fact]
    public void ConstructorLetterboxesWideImageVertically()
    {
        UniformImageViewport viewport = new(new ImageSize(200, 100), 300d, 300d);

        AssertViewportRectEqual(
            new ViewportRect(0d, 75d, 300d, 150d),
            viewport.ContentBounds);
    }

    [Fact]
    public void ConstructorPillarboxesTallImageHorizontally()
    {
        UniformImageViewport viewport = new(new ImageSize(100, 200), 300d, 300d);

        AssertViewportRectEqual(
            new ViewportRect(75d, 0d, 150d, 300d),
            viewport.ContentBounds);
    }

    [Fact]
    public void TryMapToNormalizedMapsContentCorners()
    {
        UniformImageViewport viewport = new(new ImageSize(200, 100), 300d, 300d);

        Assert.True(viewport.TryMapToNormalized(new ViewportPoint(0d, 75d), out NormalizedPoint topLeft));
        Assert.True(viewport.TryMapToNormalized(new ViewportPoint(300d, 225d), out NormalizedPoint bottomRight));
        Assert.Equal(new NormalizedPoint(0d, 0d), topLeft);
        Assert.Equal(new NormalizedPoint(1d, 1d), bottomRight);
    }

    [Fact]
    public void TryMapToNormalizedRejectsLetterboxArea()
    {
        UniformImageViewport viewport = new(new ImageSize(200, 100), 300d, 300d);

        Assert.False(viewport.TryMapToNormalized(new ViewportPoint(150d, 20d), out _));
    }

    [Fact]
    public void MapToViewportMapsNormalizedRectangleIntoContent()
    {
        UniformImageViewport viewport = new(new ImageSize(200, 100), 300d, 300d);
        NormalizedRect rectangle = new(0.25d, 0.2d, 0.75d, 0.8d);

        ViewportRect mapped = viewport.MapToViewport(rectangle);

        AssertViewportRectEqual(
            new ViewportRect(75d, 105d, 150d, 90d),
            mapped);
    }


    [Fact]
    public void MapClampedToNormalizedClampsOutsidePointerToImageEdge()
    {
        UniformImageViewport viewport = new(new ImageSize(200, 100), 300d, 300d);

        NormalizedPoint point = viewport.MapClampedToNormalized(new ViewportPoint(400d, 20d));

        Assert.Equal(new NormalizedPoint(1d, 0d), point);
    }

    [Theory]
    [InlineData(0d, 100d)]
    [InlineData(100d, 0d)]
    [InlineData(double.NaN, 100d)]
    public void ConstructorRejectsInvalidViewportSize(double width, double height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UniformImageViewport(new ImageSize(10, 10), width, height));
    }

    private static void AssertViewportRectEqual(ViewportRect expected, ViewportRect actual)
    {
        const int precision = 12;

        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
        Assert.Equal(expected.Width, actual.Width, precision);
        Assert.Equal(expected.Height, actual.Height, precision);
    }
}
