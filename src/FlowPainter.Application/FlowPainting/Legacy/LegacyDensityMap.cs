using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.FlowPainting.Legacy;

public sealed class LegacyDensityMap
{
    private readonly double[] _values;

    public LegacyDensityMap(ImageSize size, ReadOnlySpan<double> values)
    {
        if (values.Length != size.PixelCount)
        {
            throw new ArgumentException(
                $"Expected {size.PixelCount} density values but received {values.Length}.",
                nameof(values));
        }

        _values = values.ToArray();
        for (int index = 0; index < _values.Length; index++)
        {
            double value = _values[index];
            if (!double.IsFinite(value) || value <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    value,
                    $"Density value at index {index} must be finite and greater than zero.");
            }
        }

        Size = size;
    }

    public ImageSize Size { get; }

    public double this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Size.Width);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Size.Height);

            return _values[checked((y * Size.Width) + x)];
        }
    }

    public double SampleNearest(NormalizedPoint point)
    {
        int x = Math.Min((int)Math.Floor(point.X * Size.Width), Size.Width - 1);
        int y = Math.Min((int)Math.Floor(point.Y * Size.Height), Size.Height - 1);

        return this[x, y];
    }

    public double[] CopyValues()
    {
        return (double[])_values.Clone();
    }
}
