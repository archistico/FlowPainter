using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionSegmentationEstimator
{
    public const int ColorBufferBytesPerPixel = 12;
    public const int DistanceBufferBytesPerPixel = 4;
    public const int AssignmentBufferBytesPerPixel = 4;
    public const int SmoothingBufferBytesPerPixel = 4;
    public const int ConnectivityBufferBytesPerPixel = 8;
    public const int EstimatedConnectivityBytesPerRegion = 160;
    public const int DescriptorLightnessBufferBytesPerPixel = RegionDescriptorCalculator.LightnessBufferBytesPerPixel;
    public const int EstimatedDescriptorBytesPerRegion = RegionDescriptorCalculator.EstimatedWorkingBytesPerRegion;
    public const int EstimatedAdjacencyBytesPerRegion = RegionAdjacencyGraphBuilder.EstimatedWorkingBytesPerRegion;
    public const int EstimatedHierarchyBytesPerRegion = 2_048;
    public const int EstimatedBytesPerCluster = 96;
    public const int EstimatedCandidateCentersPerPixel = 9;

    public static RegionSegmentationEstimate Estimate(
        ImageSize size,
        RegionSegmentationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
        {
            int regionCount = 1;
            return new RegionSegmentationEstimate(
                regionCount,
                RegionLabelStorageKind.Compact,
                RegionLabelMap.GetRequiredBytes(size, regionCount),
                0L,
                0L,
                0L,
                0L,
                0L,
                size.GetRequiredBytes(DescriptorLightnessBufferBytesPerPixel),
                EstimatedDescriptorBytesPerRegion,
                EstimatedAdjacencyBytesPerRegion,
                EstimatedHierarchyBytesPerRegion,
                0L,
                0L);
        }

        int columns = DivideRoundUp(size.Width, settings.TargetRegionSize);
        int rows = DivideRoundUp(size.Height, settings.TargetRegionSize);
        int estimatedRegionCount = checked(columns * rows);
        estimatedRegionCount = Math.Clamp(estimatedRegionCount, 1, checked((int)size.PixelCount));

        RegionLabelStorageKind storageKind = RegionLabelMap.SelectStorage(estimatedRegionCount);
        long labelBytes = RegionLabelMap.GetRequiredBytes(size, estimatedRegionCount);
        long colorBufferBytes = size.GetRequiredBytes(ColorBufferBytesPerPixel);
        long distanceBufferBytes = size.GetRequiredBytes(DistanceBufferBytesPerPixel);
        long assignmentBufferBytes = size.GetRequiredBytes(AssignmentBufferBytesPerPixel);
        long smoothingBufferBytes = settings.PreBlurSigma > 0d
            ? size.GetRequiredBytes(SmoothingBufferBytesPerPixel)
            : 0L;
        long connectivityBufferBytes = checked(
            size.GetRequiredBytes(ConnectivityBufferBytesPerPixel)
            + ((long)estimatedRegionCount * EstimatedConnectivityBytesPerRegion));
        long descriptorBufferBytes = size.GetRequiredBytes(DescriptorLightnessBufferBytesPerPixel);
        long descriptorRegionBytes = checked(
            (long)estimatedRegionCount * EstimatedDescriptorBytesPerRegion);
        long adjacencyRegionBytes = checked(
            (long)estimatedRegionCount * EstimatedAdjacencyBytesPerRegion);
        long hierarchyRegionBytes = checked(
            (long)estimatedRegionCount * EstimatedHierarchyBytesPerRegion);
        long clusterBytes = checked((long)estimatedRegionCount * EstimatedBytesPerCluster);
        long assignmentEvaluations = checked(
            size.PixelCount
            * settings.MaximumIterations
            * EstimatedCandidateCentersPerPixel);

        return new RegionSegmentationEstimate(
            estimatedRegionCount,
            storageKind,
            labelBytes,
            colorBufferBytes,
            distanceBufferBytes,
            assignmentBufferBytes,
            smoothingBufferBytes,
            connectivityBufferBytes,
            descriptorBufferBytes,
            descriptorRegionBytes,
            adjacencyRegionBytes,
            hierarchyRegionBytes,
            clusterBytes,
            assignmentEvaluations);
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        return checked((value + divisor - 1) / divisor);
    }
}
