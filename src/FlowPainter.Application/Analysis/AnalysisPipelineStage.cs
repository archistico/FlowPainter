namespace FlowPainter.Application.Analysis;

public enum AnalysisPipelineStage
{
    Preparing,
    StructuralDetail,
    SemanticImportance,
    SemanticCorrections,
    SceneBoundaries,
    AutomaticDetail,
    ManualRegions,
    BackgroundSuppression,
    Completed
}
