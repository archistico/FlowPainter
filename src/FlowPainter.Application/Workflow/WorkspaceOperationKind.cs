namespace FlowPainter.Application.Workflow;

public enum WorkspaceOperationKind
{
    None,
    LoadingImage,
    LoadingProject,
    LoadingPreset,
    RebuildingPreview,
    AnalyzingDetail,
    PlanningStrokes,
    RenderingPreview,
    SavingProject,
    SavingPreset,
    ExportingImage
}
