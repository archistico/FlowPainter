namespace FlowPainter.Application.FlowPainting.Planning;

public sealed class DetailInfluenceSettings
{
    public const double DefaultPlacementBias = 4d;
    public const double DefaultDetailedLengthMultiplier = 0.55d;
    public const double DefaultBackgroundLengthMultiplier = 1.35d;
    public const double DefaultDetailedWidthMultiplier = 0.65d;
    public const double DefaultBackgroundWidthMultiplier = 1.45d;

    public DetailInfluenceSettings(
        double placementBias = DefaultPlacementBias,
        double detailedLengthMultiplier = DefaultDetailedLengthMultiplier,
        double backgroundLengthMultiplier = DefaultBackgroundLengthMultiplier,
        double detailedWidthMultiplier = DefaultDetailedWidthMultiplier,
        double backgroundWidthMultiplier = DefaultBackgroundWidthMultiplier)
    {
        ValidateRange(placementBias, 0d, 20d, nameof(placementBias));
        ValidateRange(detailedLengthMultiplier, 0.05d, 4d, nameof(detailedLengthMultiplier));
        ValidateRange(backgroundLengthMultiplier, 0.05d, 4d, nameof(backgroundLengthMultiplier));
        ValidateRange(detailedWidthMultiplier, 0.05d, 4d, nameof(detailedWidthMultiplier));
        ValidateRange(backgroundWidthMultiplier, 0.05d, 4d, nameof(backgroundWidthMultiplier));

        PlacementBias = placementBias;
        DetailedLengthMultiplier = detailedLengthMultiplier;
        BackgroundLengthMultiplier = backgroundLengthMultiplier;
        DetailedWidthMultiplier = detailedWidthMultiplier;
        BackgroundWidthMultiplier = backgroundWidthMultiplier;
    }

    public double PlacementBias { get; }

    public double DetailedLengthMultiplier { get; }

    public double BackgroundLengthMultiplier { get; }

    public double DetailedWidthMultiplier { get; }

    public double BackgroundWidthMultiplier { get; }

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

    private static double Interpolate(double start, double end, double amount)
    {
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
