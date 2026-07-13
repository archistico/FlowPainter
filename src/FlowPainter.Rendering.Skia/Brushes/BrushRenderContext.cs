using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Randomness;
using FlowPainter.Domain.Strokes;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal sealed class BrushRenderContext
{
    public BrushRenderContext(
        FlowStroke stroke,
        ImageSize outputSize,
        BrushSettings settings,
        float widthPixels,
        SKColor color,
        DeterministicRandom random,
        CancellationToken cancellationToken)
    {
        Stroke = stroke;
        OutputSize = outputSize;
        Settings = settings;
        WidthPixels = widthPixels;
        Color = color;
        Random = random;
        CancellationToken = cancellationToken;
    }

    public FlowStroke Stroke { get; }

    public ImageSize OutputSize { get; }

    public BrushSettings Settings { get; }

    public float WidthPixels { get; }

    public SKColor Color { get; }

    public DeterministicRandom Random { get; }

    public CancellationToken CancellationToken { get; }
}
