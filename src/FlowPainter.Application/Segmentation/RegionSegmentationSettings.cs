namespace FlowPainter.Application.Segmentation;

public sealed class RegionSegmentationSettings
{
    public const int DefaultTargetRegionSize = 64;
    public const double DefaultCompactness = 10d;
    public const double DefaultPreBlurSigma = 0.8d;
    public const int DefaultMaximumIterations = 10;
    public const double DefaultConvergenceTolerance = 0.5d;
    public const int MinimumTargetRegionSize = 4;
    public const int MaximumTargetRegionSize = 2_048;
    public const double MaximumCompactness = 1_000d;
    public const double MaximumPreBlurSigma = 10d;
    public const int MaximumAllowedIterations = 100;
    public const double MaximumConvergenceTolerance = 100d;

    public RegionSegmentationSettings(
        int targetRegionSize = DefaultTargetRegionSize,
        double compactness = DefaultCompactness,
        double preBlurSigma = DefaultPreBlurSigma,
        int maximumIterations = DefaultMaximumIterations,
        double convergenceTolerance = DefaultConvergenceTolerance,
        bool enabled = true)
    {
        if (targetRegionSize < MinimumTargetRegionSize || targetRegionSize > MaximumTargetRegionSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetRegionSize),
                targetRegionSize,
                $"Target region size must be between {MinimumTargetRegionSize} and {MaximumTargetRegionSize} pixels.");
        }

        ValidatePositiveFinite(compactness, MaximumCompactness, nameof(compactness));
        ValidateNonNegativeFinite(preBlurSigma, MaximumPreBlurSigma, nameof(preBlurSigma));

        if (maximumIterations <= 0 || maximumIterations > MaximumAllowedIterations)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIterations),
                maximumIterations,
                $"Maximum iterations must be between 1 and {MaximumAllowedIterations}.");
        }

        ValidatePositiveFinite(
            convergenceTolerance,
            MaximumConvergenceTolerance,
            nameof(convergenceTolerance));

        Enabled = enabled;
        TargetRegionSize = targetRegionSize;
        Compactness = compactness;
        PreBlurSigma = preBlurSigma;
        MaximumIterations = maximumIterations;
        ConvergenceTolerance = convergenceTolerance;
    }

    public bool Enabled { get; }

    public int TargetRegionSize { get; }

    public double Compactness { get; }

    public double PreBlurSigma { get; }

    public int MaximumIterations { get; }

    public double ConvergenceTolerance { get; }

    private static void ValidatePositiveFinite(double value, double maximum, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The value must be finite, greater than zero and no greater than {maximum}.");
        }
    }

    private static void ValidateNonNegativeFinite(double value, double maximum, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The value must be finite, non-negative and no greater than {maximum}.");
        }
    }
}
