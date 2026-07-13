namespace FlowPainter.Application.Interaction;

public readonly record struct ViewportRect
{
    public ViewportRect(double x, double y, double width, double height)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Viewport origin must be finite.");
        }

        if (!double.IsFinite(width) || width <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be finite and greater than zero.");
        }

        if (!double.IsFinite(height) || height <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be finite and greater than zero.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public bool Contains(ViewportPoint point)
    {
        return point.X >= X
            && point.X <= Right
            && point.Y >= Y
            && point.Y <= Bottom;
    }
}
