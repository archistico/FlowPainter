namespace FlowPainter.Application.Segmentation;

public static class RegionBoundaryStrengthModel
{
    public const double MeanGradientWeight = 0.35d;
    public const double MaximumGradientWeight = 0.15d;
    public const double RegionColorDifferenceWeight = 0.25d;
    public const double TextureDifferenceWeight = 0.10d;
    public const double ContinuityWeight = 0.15d;

    public const double GradientNormalizationScale = 20d;
    public const double RegionColorNormalizationScale = 30d;
    public const double TextureNormalizationScale = 15d;

    public static double Calculate(
        double meanGradient,
        double maximumGradient,
        double regionColorDifference,
        double textureDifference,
        double continuity)
    {
        ValidateNonNegative(meanGradient, nameof(meanGradient));
        ValidateNonNegative(maximumGradient, nameof(maximumGradient));
        if (maximumGradient < meanGradient)
        {
            throw new ArgumentException(
                "The maximum gradient cannot be lower than the mean gradient.",
                nameof(maximumGradient));
        }

        ValidateNonNegative(regionColorDifference, nameof(regionColorDifference));
        ValidateNonNegative(textureDifference, nameof(textureDifference));
        ValidateUnitInterval(continuity, nameof(continuity));

        double strength =
            (MeanGradientWeight * Normalize(meanGradient, GradientNormalizationScale))
            + (MaximumGradientWeight * Normalize(maximumGradient, GradientNormalizationScale))
            + (RegionColorDifferenceWeight * Normalize(
                regionColorDifference,
                RegionColorNormalizationScale))
            + (TextureDifferenceWeight * Normalize(
                textureDifference,
                TextureNormalizationScale))
            + (ContinuityWeight * continuity);
        return Math.Clamp(strength, 0d, 1d);
    }

    private static double Normalize(double value, double scale)
    {
        return value <= 0d ? 0d : value / (value + scale);
    }

    private static void ValidateNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and non-negative.");
        }
    }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between zero and one.");
        }
    }
}
