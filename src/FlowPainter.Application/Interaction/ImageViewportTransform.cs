namespace FlowPainter.Application.Interaction;

public readonly record struct ImageViewportTransform(
    double Scale,
    double TranslationX,
    double TranslationY)
{
    public static ImageViewportTransform Identity { get; } = new(1d, 0d, 0d);
}
