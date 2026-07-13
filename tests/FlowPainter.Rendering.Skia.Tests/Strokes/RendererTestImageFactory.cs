using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Tests.Strokes;

internal static class RendererTestImageFactory
{
    public static async Task<SkiaImage> LoadAsync(int width, int height, Func<int, int, SKColor> colorFactory)
    {
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
        using MemoryStream stream = new(data.ToArray(), writable: false);
        return await new SkiaImageLoader().LoadAsync(stream);
    }
}
