using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Detail;

public sealed class DetailMap
{
    private readonly float[] _values;

    public DetailMap(int width, int height, ReadOnlySpan<float> values)
    {
        ImageSize size = new(width, height);
        int expectedLength = checked(width * height);

        if (values.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Expected {expectedLength} detail values but received {values.Length}.",
                nameof(values));
        }

        _values = values.ToArray();
        for (int index = 0; index < _values.Length; index++)
        {
            float value = _values[index];
            if (!float.IsFinite(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    value,
                    $"Detail value at index {index} must be between 0 and 1.");
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

    public static DetailMap CreateUniform(ImageSize size, float detail)
    {
        if (!float.IsFinite(detail) || detail < 0f || detail > 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(detail),
                detail,
                "Detail must be finite and between 0 and 1.");
        }

        float[] values = new float[checked((int)size.PixelCount)];
        Array.Fill(values, detail);
        return new DetailMap(size.Width, size.Height, values);
    }
}
