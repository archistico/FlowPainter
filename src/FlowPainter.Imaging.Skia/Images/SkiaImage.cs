using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Images;

public sealed class SkiaImage : IRgbaPixelSource, IDisposable
{
    private SKBitmap? _bitmap;

    internal SkiaImage(SKBitmap bitmap, string? sourceName = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        _bitmap = bitmap;
        Size = new ImageSize(bitmap.Width, bitmap.Height);
        SourceName = string.IsNullOrWhiteSpace(sourceName) ? null : sourceName.Trim();
    }

    public ImageSize Size { get; }

    public string? SourceName { get; }

    public bool IsDisposed => _bitmap is null;

    public Rgba32 SampleNearest(NormalizedPoint point)
    {
        SKBitmap bitmap = GetBitmap();
        int x = Math.Min((int)Math.Floor(point.X * Size.Width), Size.Width - 1);
        int y = Math.Min((int)Math.Floor(point.Y * Size.Height), Size.Height - 1);
        SKColor color = bitmap.GetPixel(x, y);

        return new Rgba32(color.Red, color.Green, color.Blue, color.Alpha);
    }

    public byte[] EncodePng()
    {
        using SKImage image = CreateSnapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("SkiaSharp could not encode the image as PNG.");
        return data.ToArray();
    }

    internal SKBitmap GetBitmap()
    {
        return _bitmap ?? throw new ObjectDisposedException(nameof(SkiaImage));
    }

    internal SKImage CreateSnapshot()
    {
        return SKImage.FromBitmap(GetBitmap())
            ?? throw new InvalidOperationException("SkiaSharp could not create an image snapshot.");
    }

    public void Dispose()
    {
        SKBitmap? bitmap = Interlocked.Exchange(ref _bitmap, null);
        bitmap?.Dispose();
        GC.SuppressFinalize(this);
    }
}
