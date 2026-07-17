using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Images;

public static class SkiaImageFactory
{
    private const int CancellationRowBatch = 16;

    public static SkiaImage Create(
        IRgbaPixelSource source,
        string? sourceName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        SKBitmap bitmap = new(
            source.Size.Width,
            source.Size.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Unpremul);

        try
        {
            for (int y = 0; y < source.Size.Height; y++)
            {
                if (y % CancellationRowBatch == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                for (int x = 0; x < source.Size.Width; x++)
                {
                    FlowPainter.Domain.Color.Rgba32 pixel = source.SampleNearest(new NormalizedPoint(
                        (x + 0.5d) / source.Size.Width,
                        (y + 0.5d) / source.Size.Height));
                    bitmap.SetPixel(x, y, new SKColor(
                        pixel.Red,
                        pixel.Green,
                        pixel.Blue,
                        pixel.Alpha));
                }
            }

            return new SkiaImage(bitmap, sourceName);
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
