using FlowPainter.Domain.Color;

namespace FlowPainter.Application.Segmentation;

internal static class LabColorConverter
{
    public static LabColorSample Convert(Rgba32 color)
    {
        double alpha = color.Alpha / (double)byte.MaxValue;
        double red = ((color.Red / (double)byte.MaxValue) * alpha) + (1d - alpha);
        double green = ((color.Green / (double)byte.MaxValue) * alpha) + (1d - alpha);
        double blue = ((color.Blue / (double)byte.MaxValue) * alpha) + (1d - alpha);

        red = ToLinearRgb(red);
        green = ToLinearRgb(green);
        blue = ToLinearRgb(blue);

        double x = ((0.4124564d * red) + (0.3575761d * green) + (0.1804375d * blue)) / 0.95047d;
        double y = (0.2126729d * red) + (0.7151522d * green) + (0.072175d * blue);
        double z = ((0.0193339d * red) + (0.119192d * green) + (0.9503041d * blue)) / 1.08883d;

        double transformedX = TransformLabCoordinate(x);
        double transformedY = TransformLabCoordinate(y);
        double transformedZ = TransformLabCoordinate(z);
        return new LabColorSample(
            Math.Clamp((116d * transformedY) - 16d, 0d, 100d),
            500d * (transformedX - transformedY),
            200d * (transformedY - transformedZ));
    }

    private static double ToLinearRgb(double value)
    {
        return value <= 0.04045d
            ? value / 12.92d
            : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
    }

    private static double TransformLabCoordinate(double value)
    {
        const double delta = 6d / 29d;
        double threshold = delta * delta * delta;
        return value > threshold
            ? Math.Cbrt(value)
            : (value / (3d * delta * delta)) + (4d / 29d);
    }
}
