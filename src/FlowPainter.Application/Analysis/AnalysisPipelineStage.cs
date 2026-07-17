namespace FlowPainter.Application.Analysis;

public enum AnalysisPipelineStage
{
    Preparing,
    StructuralDetail,
    RegionalSegmentation,
    RegionRoles,
    SceneBoundaries,
    AutomaticDetail,
    ManualRegions,
    BackgroundSuppression,
    Completed
}
