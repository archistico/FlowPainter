namespace FlowPainter.Rendering.Skia.Strokes;

public enum StrokeRenderStage
{
    Preparing = 0,
    DrawingBackground = 1,
    DrawingStrokes = 2,
    Finalizing = 3,
    Completed = 4
}
