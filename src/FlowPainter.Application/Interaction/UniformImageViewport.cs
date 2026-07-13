using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Interaction;

public sealed class UniformImageViewport
{
    public UniformImageViewport(
        ImageSize imageSize,
        double viewportWidth,
        double viewportHeight)
    {
        if (!double.IsFinite(viewportWidth) || viewportWidth <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportWidth),
                viewportWidth,
                "Viewport width must be finite and greater than zero.");
        }

        if (!double.IsFinite(viewportHeight) || viewportHeight <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportHeight),
                viewportHeight,
                "Viewport height must be finite and greater than zero.");
        }

        ImageSize = imageSize;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        double scale = Math.Min(
            viewportWidth / imageSize.Width,
            viewportHeight / imageSize.Height);
        double contentWidth = imageSize.Width * scale;
        double contentHeight = imageSize.Height * scale;
        ContentBounds = new ViewportRect(
            (viewportWidth - contentWidth) * 0.5d,
            (viewportHeight - contentHeight) * 0.5d,
            contentWidth,
            contentHeight);
    }

    public ImageSize ImageSize { get; }

    public double ViewportWidth { get; }

    public double ViewportHeight { get; }

    public ViewportRect ContentBounds { get; }

    public bool TryMapToNormalized(
        ViewportPoint point,
        out NormalizedPoint normalizedPoint)
    {
        if (!ContentBounds.Contains(point))
        {
            normalizedPoint = default;
            return false;
        }

        normalizedPoint = new NormalizedPoint(
            Math.Clamp((point.X - ContentBounds.X) / ContentBounds.Width, 0d, 1d),
            Math.Clamp((point.Y - ContentBounds.Y) / ContentBounds.Height, 0d, 1d));
        return true;
    }

    public NormalizedPoint MapClampedToNormalized(ViewportPoint point)
    {
        return new NormalizedPoint(
            Math.Clamp((point.X - ContentBounds.X) / ContentBounds.Width, 0d, 1d),
            Math.Clamp((point.Y - ContentBounds.Y) / ContentBounds.Height, 0d, 1d));
    }

    public ViewportRect MapToViewport(NormalizedRect rectangle)
    {
        return new ViewportRect(
            ContentBounds.X + (rectangle.Left * ContentBounds.Width),
            ContentBounds.Y + (rectangle.Top * ContentBounds.Height),
            rectangle.Width * ContentBounds.Width,
            rectangle.Height * ContentBounds.Height);
    }
}
