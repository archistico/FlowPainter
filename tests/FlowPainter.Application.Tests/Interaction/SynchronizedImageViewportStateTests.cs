using FlowPainter.Application.Interaction;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Interaction;

public sealed class SynchronizedImageViewportStateTests
{
    private const int Precision = 12;

    [Fact]
    public void DefaultStateProducesIdentityTransform()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();

        ImageViewportTransform transform = state.GetTransform(viewport);

        Assert.Equal(ImageViewportTransform.Identity, transform);
        Assert.True(state.IsDefault);
    }

    [Fact]
    public void ZoomAtCenterKeepsImageCenterAtViewportCenter()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();

        state.ZoomAt(viewport, new ViewportPoint(150d, 150d), 1d);
        ImageViewportTransform transform = state.GetTransform(viewport);

        Assert.Equal(1.2d, state.Zoom, Precision);
        Assert.Equal(new NormalizedPoint(0.5d, 0.5d), state.Center);
        Assert.Equal(-30d, transform.TranslationX, Precision);
        Assert.Equal(-30d, transform.TranslationY, Precision);
    }

    [Fact]
    public void ZoomAtAnchorPreservesNormalizedPointUnderPointer()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();
        ViewportPoint anchor = new(75d, 210d);

        Assert.True(state.TryMapToNormalized(viewport, anchor, out NormalizedPoint before));
        state.ZoomAt(viewport, anchor, 2d);
        Assert.True(state.TryMapToNormalized(viewport, anchor, out NormalizedPoint after));

        AssertNormalizedPointEqual(before, after);
    }

    [Fact]
    public void PanByMovesVisibleCenterOppositeToContentMovement()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();
        state.ZoomAt(viewport, new ViewportPoint(150d, 150d), 4d);

        state.PanBy(viewport, 30d, -15d);

        Assert.True(state.Center.X < 0.5d);
        Assert.True(state.Center.Y > 0.5d);
    }

    [Fact]
    public void PanByDoesNotMoveWhenWholeImageIsVisible()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();

        state.PanBy(viewport, 100d, 100d);

        Assert.Equal(new NormalizedPoint(0.5d, 0.5d), state.Center);
    }

    [Fact]
    public void PanByClampsImageToViewportEdges()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();
        state.ZoomAt(viewport, new ViewportPoint(150d, 150d), 10d);

        state.PanBy(viewport, 100_000d, 100_000d);

        double expectedMinimum = 1d / (2d * state.Zoom);
        Assert.Equal(expectedMinimum, state.Center.X, Precision);
        Assert.Equal(expectedMinimum, state.Center.Y, Precision);
    }

    [Fact]
    public void SharedStateCentersSameImagePointInDifferentViewports()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport first = new(new ImageSize(400, 200), 500d, 300d);
        UniformImageViewport second = new(new ImageSize(400, 200), 800d, 500d);
        state.ZoomAt(first, new ViewportPoint(250d, 150d), 5d);
        state.PanBy(first, -70d, 25d);

        ImageViewportTransform firstTransform = state.GetTransform(first);
        ImageViewportTransform secondTransform = state.GetTransform(second);
        ViewportPoint firstMapped = MapNormalizedPoint(first, firstTransform, state.Center);
        ViewportPoint secondMapped = MapNormalizedPoint(second, secondTransform, state.Center);

        Assert.Equal(first.ViewportWidth * 0.5d, firstMapped.X, Precision);
        Assert.Equal(first.ViewportHeight * 0.5d, firstMapped.Y, Precision);
        Assert.Equal(second.ViewportWidth * 0.5d, secondMapped.X, Precision);
        Assert.Equal(second.ViewportHeight * 0.5d, secondMapped.Y, Precision);
    }

    [Fact]
    public void MapClampedToNormalizedAccountsForZoomAndPan()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();
        state.ZoomAt(viewport, new ViewportPoint(150d, 150d), 4d);
        state.PanBy(viewport, -40d, 20d);

        NormalizedPoint mapped = state.MapClampedToNormalized(
            viewport,
            new ViewportPoint(150d, 150d));

        AssertNormalizedPointEqual(state.Center, mapped);
    }

    [Fact]
    public void ResetRestoresIdentityState()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();
        state.ZoomAt(viewport, new ViewportPoint(100d, 100d), 3d);
        state.PanBy(viewport, 20d, 10d);

        state.Reset();

        Assert.Equal(SynchronizedImageViewportState.MinimumZoom, state.Zoom);
        Assert.Equal(new NormalizedPoint(0.5d, 0.5d), state.Center);
        Assert.Equal(ImageViewportTransform.Identity, state.GetTransform(viewport));
        Assert.True(state.IsDefault);
    }

    [Fact]
    public void ZoomAtClampsToSupportedRange()
    {
        SynchronizedImageViewportState state = new();
        UniformImageViewport viewport = CreateSquareViewport();

        state.ZoomAt(viewport, new ViewportPoint(150d, 150d), 1_000d);
        Assert.Equal(SynchronizedImageViewportState.MaximumZoom, state.Zoom);

        state.ZoomAt(viewport, new ViewportPoint(150d, 150d), -1_000d);
        Assert.Equal(SynchronizedImageViewportState.MinimumZoom, state.Zoom);
    }

    private static UniformImageViewport CreateSquareViewport()
    {
        return new UniformImageViewport(new ImageSize(300, 300), 300d, 300d);
    }

    private static ViewportPoint MapNormalizedPoint(
        UniformImageViewport viewport,
        ImageViewportTransform transform,
        NormalizedPoint point)
    {
        ViewportRect content = viewport.ContentBounds;
        double baseX = content.X + (point.X * content.Width);
        double baseY = content.Y + (point.Y * content.Height);
        return new ViewportPoint(
            (baseX * transform.Scale) + transform.TranslationX,
            (baseY * transform.Scale) + transform.TranslationY);
    }

    private static void AssertNormalizedPointEqual(
        NormalizedPoint expected,
        NormalizedPoint actual)
    {
        Assert.Equal(expected.X, actual.X, Precision);
        Assert.Equal(expected.Y, actual.Y, Precision);
    }
}
