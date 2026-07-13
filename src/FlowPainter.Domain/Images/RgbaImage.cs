using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Images;

public sealed class RgbaImage : IRgbaPixelSource
{
    private readonly Rgba32[] _pixels;

    public RgbaImage(ImageSize size, ReadOnlySpan<Rgba32> pixels)
    {
        if (pixels.Length != size.PixelCount)
        {
            throw new ArgumentException(
                $"Expected {size.PixelCount} pixels but received {pixels.Length}.",
                nameof(pixels));
        }

        Size = size;
        _pixels = pixels.ToArray();
    }

    public ImageSize Size { get; }

    public Rgba32 this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Size.Width);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Size.Height);

            return _pixels[checked((y * Size.Width) + x)];
        }
    }

    public Rgba32 SampleNearest(NormalizedPoint point)
    {
        int x = Math.Min((int)Math.Floor(point.X * Size.Width), Size.Width - 1);
        int y = Math.Min((int)Math.Floor(point.Y * Size.Height), Size.Height - 1);

        return this[x, y];
    }

    public Rgba32[] CopyPixels()
    {
        return (Rgba32[])_pixels.Clone();
    }
}
