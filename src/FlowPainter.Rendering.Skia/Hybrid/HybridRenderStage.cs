namespace FlowPainter.Rendering.Skia.Hybrid;

public enum HybridRenderStage
{
    RenderingPrimitives = 0,
    RenderingFlowStrokes = 1,
    RenderingRefinementStrokes = 2,
    Completed = 3
}
