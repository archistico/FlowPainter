namespace FlowPainter.Application.FlowPainting.Planning;

public sealed class DetailInfluenceSettings
{
    public const double DefaultPlacementBias = 4d;
    public const double DefaultDetailedLengthMultiplier = 0.55d;
    public const double DefaultBackgroundLengthMultiplier = 1.35d;
    public const double DefaultDetailedWidthMultiplier = 0.65d;
    public const double DefaultBackgroundWidthMultiplier = 1.45d;
    public const double DefaultRegionTransitionWidth = 0.05d;
    public const double DefaultDetailedSegmentMultiplier = 1.65d;
    public const double DefaultBackgroundSegmentMultiplier = 0.85d;
    public const double DefaultDetailedCurveMultiplier = 1.25d;
    public const double DefaultBackgroundCurveMultiplier = 0.85d;
    public const double DefaultDetailedTangentAlignmentBoost = 0.22d;
    public const double DefaultDetailedCrossingResistanceBoost = 0.28d;

    public DetailInfluenceSettings(
        double placementBias = DefaultPlacementBias,
        double detailedLengthMultiplier = DefaultDetailedLengthMultiplier,
        double backgroundLengthMultiplier = DefaultBackgroundLengthMultiplier,
        double detailedWidthMultiplier = DefaultDetailedWidthMultiplier,
        double backgroundWidthMultiplier = DefaultBackgroundWidthMultiplier,
        double regionTransitionWidth = DefaultRegionTransitionWidth,
        double detailedSegmentMultiplier = DefaultDetailedSegmentMultiplier,
        double backgroundSegmentMultiplier = DefaultBackgroundSegmentMultiplier,
        double detailedCurveMultiplier = DefaultDetailedCurveMultiplier,
        double backgroundCurveMultiplier = DefaultBackgroundCurveMultiplier,
        double detailedTangentAlignmentBoost = DefaultDetailedTangentAlignmentBoost,
        double detailedCrossingResistanceBoost = DefaultDetailedCrossingResistanceBoost)
    {
        ValidateRange(placementBias, 0d, 20d, nameof(placementBias));
        ValidateMultiplier(detailedLengthMultiplier, nameof(detailedLengthMultiplier));
        ValidateMultiplier(backgroundLengthMultiplier, nameof(backgroundLengthMultiplier));
        ValidateMultiplier(detailedWidthMultiplier, nameof(detailedWidthMultiplier));
        ValidateMultiplier(backgroundWidthMultiplier, nameof(backgroundWidthMultiplier));
        ValidateRange(regionTransitionWidth, 0d, 0.5d, nameof(regionTransitionWidth));
        ValidateMultiplier(detailedSegmentMultiplier, nameof(detailedSegmentMultiplier));
        ValidateMultiplier(backgroundSegmentMultiplier, nameof(backgroundSegmentMultiplier));
        ValidateMultiplier(detailedCurveMultiplier, nameof(detailedCurveMultiplier));
        ValidateMultiplier(backgroundCurveMultiplier, nameof(backgroundCurveMultiplier));
        ValidateRange(detailedTangentAlignmentBoost, 0d, 1d, nameof(detailedTangentAlignmentBoost));
        ValidateRange(detailedCrossingResistanceBoost, 0d, 1d, nameof(detailedCrossingResistanceBoost));

        PlacementBias = placementBias;
        DetailedLengthMultiplier = detailedLengthMultiplier;
        BackgroundLengthMultiplier = backgroundLengthMultiplier;
        DetailedWidthMultiplier = detailedWidthMultiplier;
        BackgroundWidthMultiplier = backgroundWidthMultiplier;
        RegionTransitionWidth = regionTransitionWidth;
        DetailedSegmentMultiplier = detailedSegmentMultiplier;
        BackgroundSegmentMultiplier = backgroundSegmentMultiplier;
        DetailedCurveMultiplier = detailedCurveMultiplier;
        BackgroundCurveMultiplier = backgroundCurveMultiplier;
        DetailedTangentAlignmentBoost = detailedTangentAlignmentBoost;
        DetailedCrossingResistanceBoost = detailedCrossingResistanceBoost;
    }

    public double PlacementBias { get; }

    public double DetailedLengthMultiplier { get; }

    public double BackgroundLengthMultiplier { get; }

    public double DetailedWidthMultiplier { get; }

    public double BackgroundWidthMultiplier { get; }

    /// <summary>
    /// Gets the feather radius around manual detail-region borders, expressed as a fraction
    /// of the shorter analysis-map dimension. The transition extends both inside and outside
    /// each region and uses a smooth-step curve.
    /// </summary>
    public double RegionTransitionWidth { get; }

    public double DetailedSegmentMultiplier { get; }

    public double BackgroundSegmentMultiplier { get; }

    public double DetailedCurveMultiplier { get; }

    public double BackgroundCurveMultiplier { get; }

    public double DetailedTangentAlignmentBoost { get; }

    public double DetailedCrossingResistanceBoost { get; }

    public double GetPlacementWeight(double detail)
    {
        ValidateDetail(detail);
        return 1d + (PlacementBias * detail);
    }

    public double GetLengthMultiplier(double detail)
    {
        ValidateDetail(detail);
        return Interpolate(BackgroundLengthMultiplier, DetailedLengthMultiplier, detail);
    }

    public double GetWidthMultiplier(double detail)
    {
        ValidateDetail(detail);
        return Interpolate(BackgroundWidthMultiplier, DetailedWidthMultiplier, detail);
    }

    public double GetSegmentMultiplier(double detail)
    {
        ValidateDetail(detail);
        return Interpolate(BackgroundSegmentMultiplier, DetailedSegmentMultiplier, detail);
    }

    public double GetCurveMultiplier(double detail)
    {
        ValidateDetail(detail);
        return Interpolate(BackgroundCurveMultiplier, DetailedCurveMultiplier, detail);
    }

    private static double Interpolate(double start, double end, double amount)
    {
        if (amount == 0d)
        {
            return start;
        }

        if (amount == 1d)
        {
            return end;
        }

        return start + ((end - start) * amount);
    }

    private static void ValidateDetail(double detail)
    {
        if (!double.IsFinite(detail) || detail < 0d || detail > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(detail),
                detail,
                "Detail must be finite and between 0 and 1.");
        }
    }

    private static void ValidateMultiplier(double value, string parameterName)
    {
        ValidateRange(value, 0.05d, 4d, parameterName);
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
