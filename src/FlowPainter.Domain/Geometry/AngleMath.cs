namespace FlowPainter.Domain.Geometry;

public static class AngleMath
{
    public const double Tau = Math.PI * 2d;

    public static double NormalizeRadians(double angle)
    {
        if (!double.IsFinite(angle))
        {
            throw new ArgumentOutOfRangeException(nameof(angle), angle, "The angle must be finite.");
        }

        double normalized = angle % Tau;
        return normalized < 0d ? normalized + Tau : normalized;
    }

    public static double ShortestDistanceRadians(double first, double second)
    {
        double normalizedFirst = NormalizeRadians(first);
        double normalizedSecond = NormalizeRadians(second);
        double directDistance = Math.Abs(normalizedFirst - normalizedSecond);

        return Math.Min(directDistance, Tau - directDistance);
    }
}
