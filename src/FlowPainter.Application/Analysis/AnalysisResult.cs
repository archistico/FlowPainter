using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Analysis;

public sealed class AnalysisResult
{
    public AnalysisResult(
        DetailMap structuralDetailMap,
        SemanticAnalysisResult semanticAnalysis,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        DetailMap automaticDetailMap,
        DetailMap manuallyComposedDetailMap,
        BackgroundSuppressionResult backgroundSuppression)
    {
        ArgumentNullException.ThrowIfNull(structuralDetailMap);
        ArgumentNullException.ThrowIfNull(semanticAnalysis);
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        ArgumentNullException.ThrowIfNull(automaticDetailMap);
        ArgumentNullException.ThrowIfNull(manuallyComposedDetailMap);
        ArgumentNullException.ThrowIfNull(backgroundSuppression);

        if (structuralDetailMap.Size != semanticAnalysis.ImportanceMap.Size
            || structuralDetailMap.Size != boundaryAnalysis.EdgeStrengthMap.Size
            || structuralDetailMap.Size != automaticDetailMap.Size
            || structuralDetailMap.Size != manuallyComposedDetailMap.Size
            || structuralDetailMap.Size != backgroundSuppression.EffectiveDetailMap.Size)
        {
            throw new ArgumentException("All analysis results must have identical dimensions.");
        }

        StructuralDetailMap = structuralDetailMap;
        SemanticAnalysis = semanticAnalysis;
        BoundaryAnalysis = boundaryAnalysis;
        AutomaticDetailMap = automaticDetailMap;
        ManuallyComposedDetailMap = manuallyComposedDetailMap;
        BackgroundSuppression = backgroundSuppression;
    }

    public DetailMap StructuralDetailMap { get; }

    public SemanticAnalysisResult SemanticAnalysis { get; }

    public SceneBoundaryAnalysisResult BoundaryAnalysis { get; }

    public DetailMap AutomaticDetailMap { get; }

    public DetailMap ManuallyComposedDetailMap { get; }

    public BackgroundSuppressionResult BackgroundSuppression { get; }
}
