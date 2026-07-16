namespace FlowPainter.Application.Hybrid;

public sealed record HybridPlanningProgress(
    HybridPlanningStage Stage,
    double Fraction,
    string Message);
