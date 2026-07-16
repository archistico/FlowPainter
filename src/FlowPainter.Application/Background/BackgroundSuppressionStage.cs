namespace FlowPainter.Application.Background;

public enum BackgroundSuppressionStage
{
    Preparing = 0,
    BuildingProtection = 1,
    EstimatingSuppression = 2,
    SmoothingTransitions = 3,
    CombiningDetail = 4,
    Completed = 5
}
