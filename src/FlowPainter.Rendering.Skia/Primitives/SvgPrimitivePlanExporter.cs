using System.Globalization;
using System.Text;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Rendering.Skia.Primitives;

public static class SvgPrimitivePlanExporter
{
    public static async Task ExportAsync(
        PrimitivePlan plan,
        ImageSize outputSize,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (destination.CanSeek)
        {
            destination.Position = 0L;
            destination.SetLength(0L);
        }

        using StreamWriter writer = new(
            destination,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            NewLine = "\n"
        };
        await writer.WriteLineAsync($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{outputSize.Width}\" height=\"{outputSize.Height}\" viewBox=\"0 0 {outputSize.Width} {outputSize.Height}\">").ConfigureAwait(false);
        await writer.WriteLineAsync($"  <rect width=\"100%\" height=\"100%\" fill=\"{ToHex(plan.BackgroundColor)}\" />").ConfigureAwait(false);

        foreach (GeometricPrimitive primitive in plan.Primitives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync("  " + CreateElement(primitive, outputSize)).ConfigureAwait(false);
        }

        await writer.WriteLineAsync("</svg>").ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string CreateElement(GeometricPrimitive primitive, ImageSize size)
    {
        double centerX = primitive.Center.X * size.Width;
        double centerY = primitive.Center.Y * size.Height;
        double width = primitive.Width * size.Width;
        double height = primitive.Height * size.Height;
        double rotationDegrees = primitive.RotationRadians * 180d / Math.PI;
        string fill = ToHex(primitive.Color);
        string opacity = Format(primitive.Color.Alpha / 255d);
        string transform = Math.Abs(rotationDegrees) < 1e-12
            ? string.Empty
            : $" transform=\"rotate({Format(rotationDegrees)} {Format(centerX)} {Format(centerY)})\"";

        return primitive.Kind switch
        {
            PrimitiveKind.Triangle => CreateTriangle(centerX, centerY, width, height, fill, opacity, transform),
            PrimitiveKind.Rectangle or PrimitiveKind.RotatedRectangle =>
                $"<rect x=\"{Format(centerX - (width * 0.5d))}\" y=\"{Format(centerY - (height * 0.5d))}\" width=\"{Format(width)}\" height=\"{Format(height)}\" fill=\"{fill}\" fill-opacity=\"{opacity}\"{transform} />",
            PrimitiveKind.Circle =>
                $"<circle cx=\"{Format(centerX)}\" cy=\"{Format(centerY)}\" r=\"{Format(Math.Min(width, height) * 0.5d)}\" fill=\"{fill}\" fill-opacity=\"{opacity}\" />",
            PrimitiveKind.Ellipse =>
                $"<ellipse cx=\"{Format(centerX)}\" cy=\"{Format(centerY)}\" rx=\"{Format(width * 0.5d)}\" ry=\"{Format(height * 0.5d)}\" fill=\"{fill}\" fill-opacity=\"{opacity}\"{transform} />",
            _ => throw new ArgumentOutOfRangeException(
                nameof(primitive),
                primitive.Kind,
                "Unknown primitive kind.")
        };
    }

    private static string CreateTriangle(
        double centerX,
        double centerY,
        double width,
        double height,
        string fill,
        string opacity,
        string transform)
    {
        string points = $"{Format(centerX)},{Format(centerY - (height * 0.5d))} "
            + $"{Format(centerX - (width * 0.5d))},{Format(centerY + (height * 0.5d))} "
            + $"{Format(centerX + (width * 0.5d))},{Format(centerY + (height * 0.5d))}";
        return $"<polygon points=\"{points}\" fill=\"{fill}\" fill-opacity=\"{opacity}\"{transform} />";
    }

    private static string ToHex(FlowPainter.Domain.Color.Rgba32 color)
    {
        return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }

    private static string Format(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
