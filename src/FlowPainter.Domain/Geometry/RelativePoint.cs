namespace FlowPainter.Domain.Geometry;

/// <summary>
/// Represents a point in canvas-relative coordinates, where 0 and 1 correspond
/// to the canvas edges. Legacy paths may temporarily extend outside that range.
/// </summary>
public readonly record struct RelativePoint
{
    public RelativePoint(double x, double y)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "The relative X coordinate must be finite.");
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "The relative Y coordinate must be finite.");
        }

        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public bool IsInsideCanvas => X >= 0d && X <= 1d && Y >= 0d && Y <= 1d;
}
