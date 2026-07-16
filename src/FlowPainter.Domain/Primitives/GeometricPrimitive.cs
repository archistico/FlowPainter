using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Primitives;

public sealed class GeometricPrimitive
{
    public GeometricPrimitive(
        int index,
        PrimitiveKind kind,
        NormalizedPoint center,
        double width,
        double height,
        double rotationRadians,
        Rgba32 color)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown primitive kind.");
        }

        ValidateSize(width, nameof(width));
        ValidateSize(height, nameof(height));
        if (!double.IsFinite(rotationRadians))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationRadians),
                rotationRadians,
                "Primitive rotation must be finite.");
        }

        Index = index;
        Kind = kind;
        Center = center;
        Width = width;
        Height = height;
        RotationRadians = AngleMath.NormalizeRadians(rotationRadians);
        Color = color;
    }

    public int Index { get; }

    public PrimitiveKind Kind { get; }

    public NormalizedPoint Center { get; }

    public double Width { get; }

    public double Height { get; }

    public double RotationRadians { get; }

    public Rgba32 Color { get; }

    public GeometricPrimitive WithColor(Rgba32 color)
    {
        return new GeometricPrimitive(
            Index,
            Kind,
            Center,
            Width,
            Height,
            RotationRadians,
            color);
    }

    private static void ValidateSize(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Primitive dimensions must be finite values in the (0, 1] range.");
        }
    }
}
