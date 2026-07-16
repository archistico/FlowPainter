namespace FlowPainter.Application.Boundaries;

public enum SceneBoundaryAnalysisStage
{
    Preparing = 0,
    ComputingMultiscaleEdges = 1,
    LinkingContours = 2,
    ClassifyingBoundaries = 3,
    EstimatingBackground = 4,
    SmoothingMaps = 5,
    Completed = 6
}
