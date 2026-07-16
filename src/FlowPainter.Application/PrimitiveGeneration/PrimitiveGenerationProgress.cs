namespace FlowPainter.Application.PrimitiveGeneration;

public readonly record struct PrimitiveGenerationProgress(
    PrimitiveGenerationStage Stage,
    int CompletedPrimitives,
    int RequestedPrimitives,
    double Fraction,
    double CurrentError);
