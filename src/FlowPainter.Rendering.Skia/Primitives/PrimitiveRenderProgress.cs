namespace FlowPainter.Rendering.Skia.Primitives;

public readonly record struct PrimitiveRenderProgress(
    PrimitiveRenderStage Stage,
    int CompletedPrimitives,
    int TotalPrimitives,
    double Fraction);
