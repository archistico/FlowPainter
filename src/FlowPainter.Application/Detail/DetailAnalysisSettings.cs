namespace FlowPainter.Application.Detail;

public sealed class DetailAnalysisSettings
{
    public const double DefaultBaseDetail = 0.12d;
    public const double DefaultEdgeWeight = 0.75d;
    public const double DefaultContrastWeight = 0.45d;
    public const int DefaultSmoothingRadius = 1;
    public const int MaximumSmoothingRadius = 16;

    public DetailAnalysisSettings(
        double baseDetail = DefaultBaseDetail,
        double edgeWeight = DefaultEdgeWeight,
        double contrastWeight = DefaultContrastWeight,
        int smoothingRadius = DefaultSmoothingRadius)
    {
        ValidateUnitInterval(baseDetail, nameof(baseDetail));
        ValidateNonNegative(edgeWeight, nameof(edgeWeight));
        ValidateNonNegative(contrastWeight, nameof(contrastWeight));
        ArgumentOutOfRangeException.ThrowIfNegative(smoothingRadius);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(smoothingRadius, MaximumSmoothingRadius);

        if (edgeWeight == 0d && contrastWeight == 0d)
        {
            throw new ArgumentException(
                "At least one structural-detail weight must be greater than zero.",
                nameof(edgeWeight));
        }

        BaseDetail = baseDetail;
        EdgeWeight = edgeWeight;
        ContrastWeight = contrastWeight;
        SmoothingRadius = smoothingRadius;
    }

    public double BaseDetail { get; }

    public double EdgeWeight { get; }

    public double ContrastWeight { get; }

    public int SmoothingRadius { get; }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between 0 and 1.");
        }
    }

    private static void ValidateNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 4d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between 0 and 4.");
        }
    }
}
