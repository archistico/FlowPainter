using FlowPainter.Domain.Brushes;

namespace FlowPainter.Rendering.Skia.Brushes;

internal static class SkiaBrushRendererFactory
{
    private static readonly SolidRoundBrushRenderer SolidRound = new();
    private static readonly SoftRoundBrushRenderer SoftRound = new();
    private static readonly FlatBrushRenderer Flat = new();
    private static readonly BristleBrushRenderer Bristle = new();

    public static ISkiaBrushRenderer Get(BrushKind kind)
    {
        return kind switch
        {
            BrushKind.SolidRound => SolidRound,
            BrushKind.SoftRound => SoftRound,
            BrushKind.Flat => Flat,
            BrushKind.Bristle => Bristle,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown brush kind.")
        };
    }
}
