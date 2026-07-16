using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Detail;

public sealed class ArtisticDetailField
{
    private readonly float[] _values;

    public ArtisticDetailField(int width, int height, ReadOnlySpan<float> values)
    {
        ImageSize size = new(width, height);
        int expectedLength = checked(width * height);
        if (values.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Expected {expectedLength} artistic-detail values but received {values.Length}.",
                nameof(values));
        }

        _values = values.ToArray();
        for (int index = 0; index < _values.Length; index++)
        {
            float value = _values[index];
            if (!float.IsFinite(value) || value < -1f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    value,
                    $"Artistic-detail value at index {index} must be between -1 and 1.");
            }
        }

        Size = size;
        Width = size.Width;
        Height = size.Height;
    }

    public ImageSize Size { get; }

    public int Width { get; }

    public int Height { get; }

    public float this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
            return _values[checked((y * Width) + x)];
        }
    }

    public float SampleNearest(NormalizedPoint point)
    {
        int x = Math.Min((int)Math.Floor(point.X * Width), Width - 1);
        int y = Math.Min((int)Math.Floor(point.Y * Height), Height - 1);
        return this[x, y];
    }

    public float[] CopyValues()
    {
        return (float[])_values.Clone();
    }

    public static ArtisticDetailField CreateUniform(ImageSize size, float value)
    {
        if (!float.IsFinite(value) || value < -1f || value > 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Artistic detail must be finite and between -1 and 1.");
        }

        float[] values = new float[checked((int)size.PixelCount)];
        Array.Fill(values, value);
        return new ArtisticDetailField(size.Width, size.Height, values);
    }
}
