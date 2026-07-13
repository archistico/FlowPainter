using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal static class SkiaBrushPath
{
    public static SKPath Create(FlowStroke stroke, ImageSize outputSize)
    {
        using SKPathBuilder builder = new();
        RelativePoint first = stroke.Points[0];
        builder.MoveTo(
            (float)(first.X * outputSize.Width),
            (float)(first.Y * outputSize.Height));

        for (int index = 1; index < stroke.Points.Count; index++)
        {
            RelativePoint point = stroke.Points[index];
            builder.LineTo(
                (float)(point.X * outputSize.Width),
                (float)(point.Y * outputSize.Height));
        }

        return builder.Detach();
    }

    public static SKPoint[] CreatePixelPoints(FlowStroke stroke, ImageSize outputSize)
    {
        SKPoint[] points = new SKPoint[stroke.Points.Count];
        for (int index = 0; index < stroke.Points.Count; index++)
        {
            RelativePoint point = stroke.Points[index];
            points[index] = new SKPoint(
                (float)(point.X * outputSize.Width),
                (float)(point.Y * outputSize.Height));
        }

        return points;
    }

    public static SKPath CreateOffset(SKPoint[] points, float offset)
    {
        using SKPathBuilder builder = new();
        for (int index = 0; index < points.Length; index++)
        {
            SKPoint normal = CalculateNormal(points, index);
            float x = points[index].X + (normal.X * offset);
            float y = points[index].Y + (normal.Y * offset);

            if (index == 0)
            {
                builder.MoveTo(x, y);
            }
            else
            {
                builder.LineTo(x, y);
            }
        }

        return builder.Detach();
    }

    private static SKPoint CalculateNormal(SKPoint[] points, int index)
    {
        int previousIndex = Math.Max(0, index - 1);
        int nextIndex = Math.Min(points.Length - 1, index + 1);
        float deltaX = points[nextIndex].X - points[previousIndex].X;
        float deltaY = points[nextIndex].Y - points[previousIndex].Y;
        float length = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (length <= float.Epsilon)
        {
            return new SKPoint(0f, 1f);
        }

        return new SKPoint(-deltaY / length, deltaX / length);
    }
}
