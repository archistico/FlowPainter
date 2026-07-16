namespace FlowPainter.Domain.Boundaries;

public readonly record struct BoundaryVector
{
    private const double MinimumLength = 1e-12d;

    public BoundaryVector(double x, double y)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "The vector X component must be finite.");
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "The vector Y component must be finite.");
        }

        double length = Math.Sqrt((x * x) + (y * y));
        if (length <= MinimumLength)
        {
            X = 0d;
            Y = 0d;
            return;
        }

        X = x / length;
        Y = y / length;
    }

    public double X { get; }

    public double Y { get; }

    public bool IsDefined => X != 0d || Y != 0d;

    public double Dot(BoundaryVector other)
    {
        return (X * other.X) + (Y * other.Y);
    }
}
