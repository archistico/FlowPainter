using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Semantics;

public static class SemanticDetailMapComposer
{
    public static DetailMap Combine(
        DetailMap structuralMap,
        SemanticAnalysisResult semanticAnalysis,
        SemanticAnalysisSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(structuralMap);
        ArgumentNullException.ThrowIfNull(semanticAnalysis);
        ArgumentNullException.ThrowIfNull(settings);

        if (structuralMap.Size != semanticAnalysis.ImportanceMap.Size)
        {
            throw new ArgumentException(
                "Structural and semantic maps must have identical dimensions.",
                nameof(semanticAnalysis));
        }

        float[] structural = structuralMap.CopyValues();
        if (!settings.Enabled || settings.OverallInfluence == 0d)
        {
            return new DetailMap(structuralMap.Width, structuralMap.Height, structural);
        }

        float[] semantic = semanticAnalysis.ImportanceMap.CopyValues();
        float influence = (float)settings.OverallInfluence;
        for (int index = 0; index < structural.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float current = structural[index];
            float semanticBoost = semantic[index] * influence;
            structural[index] = current + (semanticBoost * (1f - current));
        }

        return new DetailMap(structuralMap.Width, structuralMap.Height, structural);
    }
}
