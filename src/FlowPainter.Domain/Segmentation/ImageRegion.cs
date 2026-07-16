namespace FlowPainter.Domain.Segmentation;

public sealed class ImageRegion
{
    public ImageRegion(
        int id,
        int pixelCount,
        double normalizedArea,
        PixelBounds bounds,
        RegionCentroid centroid,
        RegionVisualDescriptors? descriptors = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelCount);

        if (!double.IsFinite(normalizedArea) || normalizedArea <= 0d || normalizedArea > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalizedArea),
                normalizedArea,
                "Normalized area must be finite, greater than zero and no greater than one.");
        }

        if (centroid.X < bounds.Left
            || centroid.X >= bounds.Right
            || centroid.Y < bounds.Top
            || centroid.Y >= bounds.Bottom)
        {
            throw new ArgumentException("The centroid must lie inside the region bounds.", nameof(centroid));
        }

        Id = id;
        PixelCount = pixelCount;
        NormalizedArea = normalizedArea;
        Bounds = bounds;
        Centroid = centroid;
        Descriptors = descriptors ?? RegionVisualDescriptors.Empty;
    }

    public int Id { get; }

    public int PixelCount { get; }

    public double NormalizedArea { get; }

    public PixelBounds Bounds { get; }

    public RegionCentroid Centroid { get; }

    public RegionVisualDescriptors Descriptors { get; }
}
