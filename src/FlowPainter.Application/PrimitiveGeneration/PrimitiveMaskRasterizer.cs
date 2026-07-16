using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class PrimitiveMaskRasterizer : IPrimitiveMaskRasterizer
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The rasterizer is replaceable through IPrimitiveMaskRasterizer and intentionally has instance semantics.")]
    public PrimitiveRasterMask Rasterize(GeometricPrimitive primitive, ImageSize size)
    {
        ArgumentNullException.ThrowIfNull(primitive);

        double centerX = primitive.Center.X * size.Width;
        double centerY = primitive.Center.Y * size.Height;
        double width = primitive.Width * size.Width;
        double height = primitive.Height * size.Height;
        double halfDiagonal = 0.5d * Math.Sqrt((width * width) + (height * height));
        int minimumX = Math.Max(0, (int)Math.Floor(centerX - halfDiagonal));
        int maximumX = Math.Min(size.Width - 1, (int)Math.Ceiling(centerX + halfDiagonal));
        int minimumY = Math.Max(0, (int)Math.Floor(centerY - halfDiagonal));
        int maximumY = Math.Min(size.Height - 1, (int)Math.Ceiling(centerY + halfDiagonal));
        double cosine = Math.Cos(primitive.RotationRadians);
        double sine = Math.Sin(primitive.RotationRadians);
        List<int> indices = [];

        for (int y = minimumY; y <= maximumY; y++)
        {
            for (int x = minimumX; x <= maximumX; x++)
            {
                double deltaX = (x + 0.5d) - centerX;
                double deltaY = (y + 0.5d) - centerY;
                double localX = (cosine * deltaX) + (sine * deltaY);
                double localY = (-sine * deltaX) + (cosine * deltaY);
                if (Contains(primitive, localX, localY, width, height))
                {
                    indices.Add(checked((y * size.Width) + x));
                }
            }
        }

        return new PrimitiveRasterMask(size, indices);
    }

    private static bool Contains(
        GeometricPrimitive primitive,
        double x,
        double y,
        double width,
        double height)
    {
        double halfWidth = width * 0.5d;
        double halfHeight = height * 0.5d;
        return primitive.Kind switch
        {
            PrimitiveKind.Triangle => ContainsTriangle(x, y, halfWidth, halfHeight),
            PrimitiveKind.Rectangle or PrimitiveKind.RotatedRectangle =>
                Math.Abs(x) <= halfWidth && Math.Abs(y) <= halfHeight,
            PrimitiveKind.Circle => ContainsEllipse(
                x,
                y,
                Math.Min(halfWidth, halfHeight),
                Math.Min(halfWidth, halfHeight)),
            PrimitiveKind.Ellipse => ContainsEllipse(x, y, halfWidth, halfHeight),
            _ => throw new ArgumentOutOfRangeException(
                nameof(primitive),
                primitive.Kind,
                "Unknown primitive kind.")
        };
    }

    private static bool ContainsTriangle(double x, double y, double halfWidth, double halfHeight)
    {
        if (y < -halfHeight || y > halfHeight)
        {
            return false;
        }

        double widthAtY = halfWidth * ((y + halfHeight) / (2d * halfHeight));
        return Math.Abs(x) <= widthAtY;
    }

    private static bool ContainsEllipse(double x, double y, double radiusX, double radiusY)
    {
        double normalizedX = x / radiusX;
        double normalizedY = y / radiusY;
        return (normalizedX * normalizedX) + (normalizedY * normalizedY) <= 1d;
    }
}
