using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Strokes;

public sealed class FlowStroke
{
    private readonly IReadOnlyList<RelativePoint> _points;

    public FlowStroke(
        int index,
        IEnumerable<RelativePoint> points,
        Rgba32 color,
        double widthRelativeToReference)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(points);

        RelativePoint[] copiedPoints = points.ToArray();
        if (copiedPoints.Length < 2)
        {
            throw new ArgumentException("A flow stroke must contain at least two points.", nameof(points));
        }

        if (!double.IsFinite(widthRelativeToReference) || widthRelativeToReference <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(widthRelativeToReference),
                widthRelativeToReference,
                "Stroke width must be a finite positive ratio.");
        }

        Index = index;
        _points = Array.AsReadOnly(copiedPoints);
        Color = color;
        WidthRelativeToReference = widthRelativeToReference;
    }

    public int Index { get; }

    public IReadOnlyList<RelativePoint> Points => _points;

    public Rgba32 Color { get; }

    public double WidthRelativeToReference { get; }
}
