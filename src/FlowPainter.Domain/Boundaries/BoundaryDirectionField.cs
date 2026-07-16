using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Boundaries;

public sealed class BoundaryDirectionField
{
    private readonly BoundaryVector[] _vectors;

    public BoundaryDirectionField(
        int width,
        int height,
        IReadOnlyList<BoundaryVector> vectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        Size = new ImageSize(width, height);
        if (vectors.Count != Size.PixelCount)
        {
            throw new ArgumentException(
                $"The direction field requires exactly {Size.PixelCount:N0} vectors.",
                nameof(vectors));
        }

        _vectors = vectors.ToArray();
    }

    public ImageSize Size { get; }

    public int Width => Size.Width;

    public int Height => Size.Height;

    public BoundaryVector this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
            return _vectors[checked((y * Width) + x)];
        }
    }

    public BoundaryVector SampleNearest(NormalizedPoint point)
    {
        int x = Math.Min(Width - 1, checked((int)(point.X * Width)));
        int y = Math.Min(Height - 1, checked((int)(point.Y * Height)));
        return this[x, y];
    }

    public BoundaryVector[] CopyVectors()
    {
        return (BoundaryVector[])_vectors.Clone();
    }

    public static BoundaryDirectionField CreateEmpty(ImageSize size)
    {
        BoundaryVector[] vectors = new BoundaryVector[checked((int)size.PixelCount)];
        return new BoundaryDirectionField(size.Width, size.Height, vectors);
    }
}
