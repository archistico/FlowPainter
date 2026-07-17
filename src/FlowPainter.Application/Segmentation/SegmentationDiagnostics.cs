namespace FlowPainter.Application.Segmentation;

public sealed class SegmentationDiagnostics
{
    public SegmentationDiagnostics(
        int iterationCount,
        bool converged,
        double finalMaximumDisplacement,
        int rawRegionCount,
        int finalRegionCount,
        int disconnectedComponentsRepaired = 0,
        int undersizedComponentsMerged = 0,
        RegionSizeDistribution? regionSizes = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(iterationCount);
        if (!double.IsFinite(finalMaximumDisplacement) || finalMaximumDisplacement < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalMaximumDisplacement),
                finalMaximumDisplacement,
                "Final displacement must be finite and non-negative.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rawRegionCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(finalRegionCount);
        if (finalRegionCount > rawRegionCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalRegionCount),
                finalRegionCount,
                "The final region count cannot exceed the pre-merge component count.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(disconnectedComponentsRepaired);
        ArgumentOutOfRangeException.ThrowIfNegative(undersizedComponentsMerged);

        IterationCount = iterationCount;
        Converged = converged;
        FinalMaximumDisplacement = finalMaximumDisplacement;
        RawRegionCount = rawRegionCount;
        FinalRegionCount = finalRegionCount;
        DisconnectedComponentsRepaired = disconnectedComponentsRepaired;
        UndersizedComponentsMerged = undersizedComponentsMerged;
        RegionSizes = regionSizes;
    }

    public int IterationCount { get; }

    public bool Converged { get; }

    public double FinalMaximumDisplacement { get; }

    public int RawRegionCount { get; }

    public int FinalRegionCount { get; }

    public int DisconnectedComponentsRepaired { get; }

    public int UndersizedComponentsMerged { get; }

    public RegionSizeDistribution? RegionSizes { get; }
}
