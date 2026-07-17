using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public sealed class RegionConnectivityResult
{
    public RegionConnectivityResult(
        RegionLabelMap labels,
        int rawComponentCount,
        int disconnectedComponentsRepaired,
        int undersizedComponentsMerged)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rawComponentCount);
        ArgumentOutOfRangeException.ThrowIfNegative(disconnectedComponentsRepaired);
        ArgumentOutOfRangeException.ThrowIfNegative(undersizedComponentsMerged);

        if (rawComponentCount < labels.RegionCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawComponentCount),
                rawComponentCount,
                "The raw component count cannot be smaller than the final region count.");
        }

        Labels = labels;
        RawComponentCount = rawComponentCount;
        DisconnectedComponentsRepaired = disconnectedComponentsRepaired;
        UndersizedComponentsMerged = undersizedComponentsMerged;
    }

    public RegionLabelMap Labels { get; }

    public int RawComponentCount { get; }

    public int DisconnectedComponentsRepaired { get; }

    public int UndersizedComponentsMerged { get; }
}
