namespace FlowPainter.Domain.Geometry;

public readonly record struct NormalizedPoint
{
    public NormalizedPoint(double x, double y)
    {
        ValidateCoordinate(x, nameof(x));
        ValidateCoordinate(y, nameof(y));

        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    private static void ValidateCoordinate(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Normalized coordinates must be finite values between 0 and 1.");
        }
    }
}
