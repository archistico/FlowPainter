namespace FlowPainter.Application.Background;

public sealed class BackgroundSuppressionSettings
{
    public const double DefaultOverallStrength = 0.78d;
    public const double DefaultDetailFloor = 0.16d;
    public const double DefaultUncertaintyProtection = 0.88d;
    public const double DefaultSilhouetteProtection = 0.96d;
    public const double DefaultTransitionSoftness = 0.72d;
    public const double DefaultBackgroundPlacementWeight = 0.32d;
    public const double DefaultStrokeLengthMultiplier = 1.7d;
    public const double DefaultStrokeWidthMultiplier = 1.55d;
    public const double DefaultSegmentMultiplier = 0.62d;
    public const double DefaultCurveFreedomMultiplier = 1.45d;
    public const double DefaultColorSimplification = 0.28d;

    public BackgroundSuppressionSettings(
        bool enabled = false,
        double overallStrength = DefaultOverallStrength,
        double detailFloor = DefaultDetailFloor,
        double uncertaintyProtection = DefaultUncertaintyProtection,
        double silhouetteProtection = DefaultSilhouetteProtection,
        double transitionSoftness = DefaultTransitionSoftness,
        double backgroundPlacementWeight = DefaultBackgroundPlacementWeight,
        double strokeLengthMultiplier = DefaultStrokeLengthMultiplier,
        double strokeWidthMultiplier = DefaultStrokeWidthMultiplier,
        double segmentMultiplier = DefaultSegmentMultiplier,
        double curveFreedomMultiplier = DefaultCurveFreedomMultiplier,
        double colorSimplification = DefaultColorSimplification)
    {
        ValidateUnitInterval(overallStrength, nameof(overallStrength));
        ValidateUnitInterval(detailFloor, nameof(detailFloor));
        ValidateUnitInterval(uncertaintyProtection, nameof(uncertaintyProtection));
        ValidateUnitInterval(silhouetteProtection, nameof(silhouetteProtection));
        ValidateUnitInterval(transitionSoftness, nameof(transitionSoftness));
        ValidateRange(backgroundPlacementWeight, 0.02d, 1d, nameof(backgroundPlacementWeight));
        ValidateRange(strokeLengthMultiplier, 1d, 4d, nameof(strokeLengthMultiplier));
        ValidateRange(strokeWidthMultiplier, 1d, 4d, nameof(strokeWidthMultiplier));
        ValidateRange(segmentMultiplier, 0.1d, 1d, nameof(segmentMultiplier));
        ValidateRange(curveFreedomMultiplier, 1d, 4d, nameof(curveFreedomMultiplier));
        ValidateUnitInterval(colorSimplification, nameof(colorSimplification));

        Enabled = enabled;
        OverallStrength = overallStrength;
        DetailFloor = detailFloor;
        UncertaintyProtection = uncertaintyProtection;
        SilhouetteProtection = silhouetteProtection;
        TransitionSoftness = transitionSoftness;
        BackgroundPlacementWeight = backgroundPlacementWeight;
        StrokeLengthMultiplier = strokeLengthMultiplier;
        StrokeWidthMultiplier = strokeWidthMultiplier;
        SegmentMultiplier = segmentMultiplier;
        CurveFreedomMultiplier = curveFreedomMultiplier;
        ColorSimplification = colorSimplification;
    }

    public bool Enabled { get; }

    public double OverallStrength { get; }

    public double DetailFloor { get; }

    public double UncertaintyProtection { get; }

    public double SilhouetteProtection { get; }

    public double TransitionSoftness { get; }

    public double BackgroundPlacementWeight { get; }

    public double StrokeLengthMultiplier { get; }

    public double StrokeWidthMultiplier { get; }

    public double SegmentMultiplier { get; }

    public double CurveFreedomMultiplier { get; }

    public double ColorSimplification { get; }

    public double GetPlacementMultiplier(double suppression)
    {
        ValidateUnitInterval(suppression, nameof(suppression));
        return Interpolate(1d, BackgroundPlacementWeight, suppression);
    }

    public double GetStrokeLengthMultiplier(double suppression)
    {
        ValidateUnitInterval(suppression, nameof(suppression));
        return Interpolate(1d, StrokeLengthMultiplier, suppression);
    }

    public double GetStrokeWidthMultiplier(double suppression)
    {
        ValidateUnitInterval(suppression, nameof(suppression));
        return Interpolate(1d, StrokeWidthMultiplier, suppression);
    }

    public double GetSegmentMultiplier(double suppression)
    {
        ValidateUnitInterval(suppression, nameof(suppression));
        return Interpolate(1d, SegmentMultiplier, suppression);
    }

    public double GetCurveFreedomMultiplier(double suppression)
    {
        ValidateUnitInterval(suppression, nameof(suppression));
        return Interpolate(1d, CurveFreedomMultiplier, suppression);
    }

    private static double Interpolate(double start, double end, double amount)
    {
        return start + ((end - start) * amount);
    }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        ValidateRange(value, 0d, 1d, parameterName);
    }

    private static void ValidateRange(double value, double minimum, double maximum, string parameterName)
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
