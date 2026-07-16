namespace FlowPainter.Domain.Segmentation;

public readonly record struct RegionCentroid
{
    public RegionCentroid(double x, double y)
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
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Region centroid coordinates must be finite and non-negative.");
        }
    }
}
