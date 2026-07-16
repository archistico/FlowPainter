namespace FlowPainter.Application.Segmentation;

public readonly record struct RegionSegmentationProgress
{
    public RegionSegmentationProgress(
        RegionSegmentationStage stage,
        double stageFraction,
        double overallFraction,
        int completedIterations = 0,
        int totalIterations = 0)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown segmentation stage.");
        }

        ValidateFraction(stageFraction, nameof(stageFraction));
        ValidateFraction(overallFraction, nameof(overallFraction));
        ArgumentOutOfRangeException.ThrowIfNegative(completedIterations);
        ArgumentOutOfRangeException.ThrowIfNegative(totalIterations);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(completedIterations, totalIterations);

        Stage = stage;
        StageFraction = stageFraction;
        OverallFraction = overallFraction;
        CompletedIterations = completedIterations;
        TotalIterations = totalIterations;
    }

    public RegionSegmentationStage Stage { get; }

    public double StageFraction { get; }

    public double OverallFraction { get; }

    public int CompletedIterations { get; }

    public int TotalIterations { get; }

    private static void ValidateFraction(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Progress fractions must be finite and between zero and one.");
        }
    }
}
