using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public readonly record struct RegionSegmentationEstimate(
    int EstimatedRegionCount,
    RegionLabelStorageKind LabelStorageKind,
    long LabelBytes,
    long ColorBufferBytes,
    long DistanceBufferBytes,
    long AssignmentBufferBytes,
    long SmoothingBufferBytes,
    long ConnectivityBufferBytes,
    long DescriptorBufferBytes,
    long DescriptorRegionBytes,
    long AdjacencyRegionBytes,
    long HierarchyRegionBytes,
    long ClusterBytes,
    long EstimatedAssignmentEvaluations)
{
    public long EstimatedPeakBytes => checked(
        LabelBytes
        + ColorBufferBytes
        + DistanceBufferBytes
        + AssignmentBufferBytes
        + SmoothingBufferBytes
        + ConnectivityBufferBytes
        + DescriptorBufferBytes
        + DescriptorRegionBytes
        + AdjacencyRegionBytes
        + HierarchyRegionBytes
        + ClusterBytes);
}
