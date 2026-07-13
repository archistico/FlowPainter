namespace FlowPainter.Domain.Geometry;

public readonly record struct NormalizedRect
{
    public NormalizedRect(double left, double top, double right, double bottom)
    {
        NormalizedPoint topLeft = new(left, top);
        NormalizedPoint bottomRight = new(right, bottom);

        if (bottomRight.X <= topLeft.X)
        {
            throw new ArgumentException("The right edge must be greater than the left edge.", nameof(right));
        }

        if (bottomRight.Y <= topLeft.Y)
        {
            throw new ArgumentException("The bottom edge must be greater than the top edge.", nameof(bottom));
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left { get; }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }

    public double Width => Right - Left;

    public double Height => Bottom - Top;

    public static NormalizedRect FromCorners(NormalizedPoint first, NormalizedPoint second)
    {
        double left = Math.Min(first.X, second.X);
        double top = Math.Min(first.Y, second.Y);
        double right = Math.Max(first.X, second.X);
        double bottom = Math.Max(first.Y, second.Y);

        return new NormalizedRect(left, top, right, bottom);
    }

    public bool Contains(NormalizedPoint point)
    {
        return point.X >= Left
            && point.X <= Right
            && point.Y >= Top
            && point.Y <= Bottom;
    }
}
