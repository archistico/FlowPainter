namespace FlowPainter.Application.Semantics;

public enum SemanticAnalysisStage
{
    Preparing = 0,
    ComputingSaliency = 1,
    SegmentingSubjects = 2,
    BuildingSilhouettes = 3,
    CombiningMaps = 4,
    Completed = 5
}
