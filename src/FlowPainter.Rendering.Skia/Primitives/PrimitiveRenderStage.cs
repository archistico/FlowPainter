namespace FlowPainter.Rendering.Skia.Primitives;

public enum PrimitiveRenderStage
{
    Preparing = 0,
    DrawingBackground = 1,
    DrawingPrimitives = 2,
    Finalizing = 3,
    Completed = 4
}
