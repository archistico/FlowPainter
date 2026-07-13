using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Tests.Images;

internal static class SkiaTestImageFactory
{
    public static byte[] CreatePng(int width, int height, Func<int, int, SKColor> colorFactory)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(colorFactory);

        SKImageInfo info = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKBitmap bitmap = new(info);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, colorFactory(x, y));
            }
        }

        using SKImage image = SKImage.FromBitmap(bitmap)
            ?? throw new InvalidOperationException("SkiaSharp could not create the test image.");
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("SkiaSharp could not encode the test image.");
        return data.ToArray();
    }
}
