using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Interaction;

public sealed class SynchronizedImageViewportState
{
    public const double MinimumZoom = 1d;
    public const double MaximumZoom = 32d;
    public const double DefaultZoomStep = 1.2d;

    private double _centerX = 0.5d;
    private double _centerY = 0.5d;

    public double Zoom { get; private set; } = MinimumZoom;

    public NormalizedPoint Center => new(_centerX, _centerY);

    public bool IsDefault => Zoom == MinimumZoom
        && _centerX == 0.5d
        && _centerY == 0.5d;

    public void Reset()
    {
        Zoom = MinimumZoom;
        _centerX = 0.5d;
        _centerY = 0.5d;
    }

    public ImageViewportTransform GetTransform(UniformImageViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        ViewportRect content = viewport.ContentBounds;
        double centerX = content.X + (_centerX * content.Width);
        double centerY = content.Y + (_centerY * content.Height);
        double translationX = (viewport.ViewportWidth * 0.5d) - (Zoom * centerX);
        double translationY = (viewport.ViewportHeight * 0.5d) - (Zoom * centerY);

        return new ImageViewportTransform(Zoom, translationX, translationY);
    }

    public void ZoomAt(
        UniformImageViewport viewport,
        ViewportPoint anchor,
        double wheelDelta,
        double zoomStep = DefaultZoomStep)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        if (!double.IsFinite(wheelDelta))
        {
            throw new ArgumentOutOfRangeException(
                nameof(wheelDelta),
                wheelDelta,
                "Wheel delta must be finite.");
        }

        if (!double.IsFinite(zoomStep) || zoomStep <= 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoomStep),
                zoomStep,
                "Zoom step must be finite and greater than one.");
        }

        if (wheelDelta == 0d)
        {
            return;
        }

        double requestedZoom = Zoom * Math.Pow(zoomStep, wheelDelta);
        double nextZoom = Math.Clamp(requestedZoom, MinimumZoom, MaximumZoom);
        if (nextZoom == Zoom)
        {
            return;
        }

        ViewportRect content = viewport.ContentBounds;
        ImageViewportTransform currentTransform = GetTransform(viewport);
        double baseAnchorX = (anchor.X - currentTransform.TranslationX) / currentTransform.Scale;
        double baseAnchorY = (anchor.Y - currentTransform.TranslationY) / currentTransform.Scale;
        double normalizedAnchorX = (baseAnchorX - content.X) / content.Width;
        double normalizedAnchorY = (baseAnchorY - content.Y) / content.Height;
        double viewportCenterX = viewport.ViewportWidth * 0.5d;
        double viewportCenterY = viewport.ViewportHeight * 0.5d;
        double nextBaseCenterX = content.X
            + (normalizedAnchorX * content.Width)
            - ((anchor.X - viewportCenterX) / nextZoom);
        double nextBaseCenterY = content.Y
            + (normalizedAnchorY * content.Height)
            - ((anchor.Y - viewportCenterY) / nextZoom);

        Zoom = nextZoom;
        SetClampedCenter(
            viewport,
            (nextBaseCenterX - content.X) / content.Width,
            (nextBaseCenterY - content.Y) / content.Height);
    }

    public void PanBy(
        UniformImageViewport viewport,
        double deltaX,
        double deltaY)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        if (!double.IsFinite(deltaX))
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaX),
                deltaX,
                "Horizontal pan delta must be finite.");
        }

        if (!double.IsFinite(deltaY))
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaY),
                deltaY,
                "Vertical pan delta must be finite.");
        }

        ViewportRect content = viewport.ContentBounds;
        SetClampedCenter(
            viewport,
            _centerX - (deltaX / (Zoom * content.Width)),
            _centerY - (deltaY / (Zoom * content.Height)));
    }

    public bool TryMapToNormalized(
        UniformImageViewport viewport,
        ViewportPoint point,
        out NormalizedPoint normalizedPoint)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        ImageViewportTransform transform = GetTransform(viewport);
        ViewportPoint untransformed = new(
            (point.X - transform.TranslationX) / transform.Scale,
            (point.Y - transform.TranslationY) / transform.Scale);
        return viewport.TryMapToNormalized(untransformed, out normalizedPoint);
    }

    public NormalizedPoint MapClampedToNormalized(
        UniformImageViewport viewport,
        ViewportPoint point)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        ImageViewportTransform transform = GetTransform(viewport);
        ViewportPoint untransformed = new(
            (point.X - transform.TranslationX) / transform.Scale,
            (point.Y - transform.TranslationY) / transform.Scale);
        return viewport.MapClampedToNormalized(untransformed);
    }

    private void SetClampedCenter(
        UniformImageViewport viewport,
        double centerX,
        double centerY)
    {
        ViewportRect content = viewport.ContentBounds;
        _centerX = ClampCenterCoordinate(
            centerX,
            viewport.ViewportWidth,
            content.Width,
            Zoom);
        _centerY = ClampCenterCoordinate(
            centerY,
            viewport.ViewportHeight,
            content.Height,
            Zoom);
    }

    private static double ClampCenterCoordinate(
        double value,
        double viewportLength,
        double contentLength,
        double zoom)
    {
        double visibleHalf = viewportLength / (2d * zoom * contentLength);
        if (visibleHalf >= 0.5d)
        {
            return 0.5d;
        }

        return Math.Clamp(value, visibleHalf, 1d - visibleHalf);
    }
}
