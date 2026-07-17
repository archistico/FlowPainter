namespace FlowPainter.Application.Segmentation;

public sealed class RegionMergeSettings
{
    public const double DefaultIntermediateTargetRatio = 0.60d;
    public const double DefaultBroadMassTargetRatio = 0.30d;
    public const double DefaultIntermediateMaximumCost = 0.42d;
    public const double DefaultBroadMassMaximumCost = 0.62d;
    public const double DefaultStrongBoundaryThreshold = 0.72d;
    public const double DefaultMaximumParentAreaFraction = 0.45d;

    public RegionMergeSettings(
        double intermediateTargetRatio = DefaultIntermediateTargetRatio,
        double broadMassTargetRatio = DefaultBroadMassTargetRatio,
        double intermediateMaximumCost = DefaultIntermediateMaximumCost,
        double broadMassMaximumCost = DefaultBroadMassMaximumCost,
        double strongBoundaryThreshold = DefaultStrongBoundaryThreshold,
        double maximumParentAreaFraction = DefaultMaximumParentAreaFraction)
    {
        ValidatePositiveUnitInterval(intermediateTargetRatio, nameof(intermediateTargetRatio));
        ValidatePositiveUnitInterval(broadMassTargetRatio, nameof(broadMassTargetRatio));
        if (broadMassTargetRatio > intermediateTargetRatio)
        {
            throw new ArgumentException(
                "The broad-mass target ratio cannot exceed the intermediate target ratio.",
                nameof(broadMassTargetRatio));
        }

        ValidateUnitInterval(intermediateMaximumCost, nameof(intermediateMaximumCost));
        ValidateUnitInterval(broadMassMaximumCost, nameof(broadMassMaximumCost));
        if (broadMassMaximumCost < intermediateMaximumCost)
        {
            throw new ArgumentException(
                "The broad-mass merge cost cannot be lower than the intermediate merge cost.",
                nameof(broadMassMaximumCost));
        }

        ValidateUnitInterval(strongBoundaryThreshold, nameof(strongBoundaryThreshold));
        ValidatePositiveUnitInterval(maximumParentAreaFraction, nameof(maximumParentAreaFraction));

        IntermediateTargetRatio = intermediateTargetRatio;
        BroadMassTargetRatio = broadMassTargetRatio;
        IntermediateMaximumCost = intermediateMaximumCost;
        BroadMassMaximumCost = broadMassMaximumCost;
        StrongBoundaryThreshold = strongBoundaryThreshold;
        MaximumParentAreaFraction = maximumParentAreaFraction;
    }

    public double IntermediateTargetRatio { get; }

    public double BroadMassTargetRatio { get; }

    public double IntermediateMaximumCost { get; }

    public double BroadMassMaximumCost { get; }

    public double StrongBoundaryThreshold { get; }

    public double MaximumParentAreaFraction { get; }

    private static void ValidatePositiveUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite, greater than zero and no greater than one.");
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
