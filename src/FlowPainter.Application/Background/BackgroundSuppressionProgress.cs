namespace FlowPainter.Application.Background;

public readonly record struct BackgroundSuppressionProgress(
    BackgroundSuppressionStage Stage,
    int CompletedRows,
    int TotalRows,
    double Fraction);
