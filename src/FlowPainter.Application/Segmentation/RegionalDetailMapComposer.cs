using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Segmentation;

public static class RegionalDetailMapComposer
{
    public const double DefaultRegionalInfluence = 0.55d;

    public static DetailMap Combine(
        DetailMap structuralMap,
        RegionalStructureAnalysisResult regionalAnalysis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(structuralMap);
        ArgumentNullException.ThrowIfNull(regionalAnalysis);
        if (structuralMap.Size != regionalAnalysis.ImportanceMap.Size)
        {
            throw new ArgumentException(
                "Structural and regional maps must have identical dimensions.",
                nameof(regionalAnalysis));
        }

        float[] structural = structuralMap.CopyValues();
        float[] regional = regionalAnalysis.ImportanceMap.CopyValues();
        for (int index = 0; index < structural.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float current = structural[index];
            float regionalBoost = (float)(regional[index] * DefaultRegionalInfluence);
            structural[index] = current + (regionalBoost * (1f - current));
        }

        return new DetailMap(structuralMap.Width, structuralMap.Height, structural);
    }
}
