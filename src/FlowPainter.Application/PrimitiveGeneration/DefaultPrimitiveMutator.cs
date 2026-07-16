using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class DefaultPrimitiveMutator : IPrimitiveMutator
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This implementation participates in a replaceable application-service contract and intentionally retains instance semantics.")]
    public GeometricPrimitive Mutate(
        GeometricPrimitive primitive,
        PrimitiveGenerationSettings settings,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(random);

        double x = primitive.Center.X;
        double y = primitive.Center.Y;
        double width = primitive.Width;
        double height = primitive.Height;
        double rotation = primitive.RotationRadians;
        int mutation = random.NextInt32(5);

        switch (mutation)
        {
            case 0:
                x = ClampCoordinate(x + RandomDelta(random, settings.MaximumSize * 0.25d));
                break;
            case 1:
                y = ClampCoordinate(y + RandomDelta(random, settings.MaximumSize * 0.25d));
                break;
            case 2:
                width = ClampSize(width * (0.75d + (0.5d * random.NextDouble())), settings);
                break;
            case 3:
                height = ClampSize(height * (0.75d + (0.5d * random.NextDouble())), settings);
                break;
            default:
                rotation = AngleMath.NormalizeRadians(rotation + RandomDelta(random, Math.PI / 6d));
                break;
        }

        if (primitive.Kind == PrimitiveKind.Circle)
        {
            double size = mutation == 3 ? height : width;
            width = size;
            height = size;
        }
        else if (primitive.Kind == PrimitiveKind.Rectangle)
        {
            rotation = 0d;
        }

        return new GeometricPrimitive(
            primitive.Index,
            primitive.Kind,
            new NormalizedPoint(x, y),
            width,
            height,
            rotation,
            primitive.Color);
    }

    private static double RandomDelta(IRandomSource random, double amplitude)
    {
        return ((random.NextDouble() * 2d) - 1d) * amplitude;
    }

    private static double ClampCoordinate(double value)
    {
        return Math.Clamp(value, 0d, 1d);
    }

    private static double ClampSize(double value, PrimitiveGenerationSettings settings)
    {
        return Math.Clamp(value, settings.MinimumSize, settings.MaximumSize);
    }
}
