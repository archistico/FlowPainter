namespace FlowPainter.Rendering.Skia.Hybrid;

public sealed record HybridRenderProgress(
    HybridRenderStage Stage,
    double Fraction,
    string Message);
