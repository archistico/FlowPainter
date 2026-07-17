namespace FlowPainter.Application.Boundaries;

public sealed class RegionalBoundaryFieldSettings
{
    public const int DefaultMaximumDistancePixels = 10;
    public const double DefaultHardBarrierThreshold = 0.62d;
    public const double DefaultHardTransitionRadiusFactor = 0.45d;
    public const double DefaultSoftTransitionExponent = 0.65d;
    public const int MaximumSupportedDistancePixels = 64;

    public RegionalBoundaryFieldSettings(
        int maximumDistancePixels = DefaultMaximumDistancePixels,
        double hardBarrierThreshold = DefaultHardBarrierThreshold,
        double hardTransitionRadiusFactor = DefaultHardTransitionRadiusFactor,
        double softTransitionExponent = DefaultSoftTransitionExponent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumDistancePixels);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            maximumDistancePixels,
            MaximumSupportedDistancePixels,
            nameof(maximumDistancePixels));
        ValidateUnitInterval(hardBarrierThreshold, nameof(hardBarrierThreshold));
        ValidateRange(
            hardTransitionRadiusFactor,
            0.1d,
            1d,
            nameof(hardTransitionRadiusFactor));
        ValidateRange(
            softTransitionExponent,
            0.2d,
            1d,
            nameof(softTransitionExponent));

        MaximumDistancePixels = maximumDistancePixels;
        HardBarrierThreshold = hardBarrierThreshold;
        HardTransitionRadiusFactor = hardTransitionRadiusFactor;
        SoftTransitionExponent = softTransitionExponent;
    }

    public int MaximumDistancePixels { get; }

    public double HardBarrierThreshold { get; }

    public double HardTransitionRadiusFactor { get; }

    public double SoftTransitionExponent { get; }

    public static RegionalBoundaryFieldSettings FromBoundaryPainting(
        BoundaryPaintingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        int maximumDistance = Math.Min(
            MaximumSupportedDistancePixels,
            checked(settings.AlignmentRadius * 2));
        return new RegionalBoundaryFieldSettings(
            maximumDistance,
            settings.HardBoundaryThreshold);
    }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        ValidateRange(value, 0d, 1d, parameterName);
    }

    private static void ValidateRange(
        double value,
        double minimum,
        double maximum,
        string parameterName)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The value must be finite and between {minimum} and {maximum}.");
        }
    }
}
