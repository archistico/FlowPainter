namespace FlowPainter.Application.Workloads;

public static class WorkloadBudgetPolicy
{
    public const long MaximumPeakWorkingSetBytes = 2L * 1024L * 1024L * 1024L;
    public const long MaximumFlowSegmentSteps = 25_000_000L;
    public const long MaximumPrimitiveScoreAttempts = 5_000_000L;
    public const long MaximumPrimitivePixelEvaluations = 3_000_000_000L;

    public static double MaximumPeakWorkingSetMebibytes =>
        MaximumPeakWorkingSetBytes / 1024d / 1024d;

    public static bool IsMemoryWithinBudget(long estimatedPeakBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedPeakBytes);
        return estimatedPeakBytes <= MaximumPeakWorkingSetBytes;
    }

    public static void EnsureMemoryWithinBudget(
        long estimatedPeakBytes,
        string operationName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedPeakBytes);
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("An operation name is required.", nameof(operationName));
        }

        if (IsMemoryWithinBudget(estimatedPeakBytes))
        {
            return;
        }

        double estimatedMebibytes = estimatedPeakBytes / 1024d / 1024d;
        throw new InvalidOperationException(
            $"{operationName.Trim()} requires an estimated {estimatedMebibytes:N0} MiB, "
            + $"above the supported {MaximumPeakWorkingSetMebibytes:N0} MiB working-set budget. "
            + "Reduce the preview or final-output size, or use a less memory-intensive mode.");
    }

    public static void EnsureGenerationWithinBudget(GenerationWorkEstimate estimate)
    {
        if (!Enum.IsDefined(estimate.Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimate),
                estimate.Mode,
                "Unknown generative mode.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(estimate.FlowSegmentSteps);
        ArgumentOutOfRangeException.ThrowIfNegative(estimate.PrimitiveScoreAttempts);
        ArgumentOutOfRangeException.ThrowIfNegative(estimate.PrimitivePixelEvaluations);

        if (estimate.FlowSegmentSteps > MaximumFlowSegmentSteps)
        {
            throw new InvalidOperationException(
                $"The requested {estimate.Mode} plan requires {estimate.FlowSegmentSteps:N0} flow-segment steps, "
                + $"above the supported {MaximumFlowSegmentSteps:N0} step budget. "
                + "Reduce the stroke count or segment count.");
        }

        if (estimate.PrimitiveScoreAttempts > MaximumPrimitiveScoreAttempts)
        {
            throw new InvalidOperationException(
                $"The requested {estimate.Mode} plan requires {estimate.PrimitiveScoreAttempts:N0} primitive score attempts, "
                + $"above the supported {MaximumPrimitiveScoreAttempts:N0} attempt budget. "
                + "Reduce primitive count, candidates or mutations.");
        }

        if (estimate.PrimitivePixelEvaluations > MaximumPrimitivePixelEvaluations)
        {
            throw new InvalidOperationException(
                $"The requested {estimate.Mode} plan requires an estimated "
                + $"{estimate.PrimitivePixelEvaluations:N0} primitive pixel evaluations, "
                + $"above the supported {MaximumPrimitivePixelEvaluations:N0} evaluation budget. "
                + "Reduce primitive size, count, candidates, mutations or preview resolution.");
        }
    }
}
