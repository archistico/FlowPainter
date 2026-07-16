using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Background;

public static class BackgroundSuppressionComposer
{
    private const int ProgressRowBatch = 16;

    public static BackgroundSuppressionResult Compose(
        DetailMap automaticDetailMap,
        DetailMap composedDetailMap,
        SemanticAnalysisResult semanticAnalysis,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        BackgroundSuppressionSettings settings,
        IProgress<BackgroundSuppressionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automaticDetailMap);
        ArgumentNullException.ThrowIfNull(composedDetailMap);
        ArgumentNullException.ThrowIfNull(semanticAnalysis);
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        ArgumentNullException.ThrowIfNull(settings);

        if (automaticDetailMap.Size != composedDetailMap.Size
            || automaticDetailMap.Size != semanticAnalysis.ImportanceMap.Size
            || automaticDetailMap.Size != boundaryAnalysis.BackgroundConfidenceMap.Size)
        {
            throw new ArgumentException("All detail, semantic and boundary maps must have identical dimensions.");
        }

        if (!settings.Enabled || settings.OverallStrength == 0d)
        {
            progress?.Report(new BackgroundSuppressionProgress(
                BackgroundSuppressionStage.Completed,
                composedDetailMap.Height,
                composedDetailMap.Height,
                1d));
            return BackgroundSuppressionResult.CreateDisabled(composedDetailMap);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new BackgroundSuppressionProgress(
            BackgroundSuppressionStage.Preparing,
            0,
            composedDetailMap.Height,
            0d));

        float[] automatic = automaticDetailMap.CopyValues();
        float[] composed = composedDetailMap.CopyValues();
        float[] importance = semanticAnalysis.ImportanceMap.CopyValues();
        float[] subjects = semanticAnalysis.SubjectMap.CopyValues();
        float[] silhouettes = semanticAnalysis.SilhouetteMap.CopyValues();
        float[] background = boundaryAnalysis.BackgroundConfidenceMap.CopyValues();
        float[] uncertainty = boundaryAnalysis.UncertaintyMap.CopyValues();
        float[] subjectBoundaries = boundaryAnalysis.SubjectBoundaryMap.CopyValues();
        float[] protection = new float[composed.Length];
        float[] suppression = new float[composed.Length];

        BuildProtection(
            automatic,
            composed,
            importance,
            subjects,
            silhouettes,
            uncertainty,
            subjectBoundaries,
            protection,
            composedDetailMap.Width,
            composedDetailMap.Height,
            settings,
            progress,
            cancellationToken);
        BuildSuppression(
            background,
            protection,
            suppression,
            composedDetailMap.Width,
            composedDetailMap.Height,
            settings,
            progress,
            cancellationToken);

        if (settings.TransitionSoftness > 0d)
        {
            int radius = checked((int)Math.Round(
                1d + (settings.TransitionSoftness * 7d),
                MidpointRounding.AwayFromZero));
            suppression = Smooth(
                suppression,
                composedDetailMap.Width,
                composedDetailMap.Height,
                radius,
                progress,
                cancellationToken);
            ReapplyProtection(
                suppression,
                protection,
                composedDetailMap.Width,
                composedDetailMap.Height,
                cancellationToken);
        }

        float[] effective = new float[composed.Length];
        float[] artistic = new float[composed.Length];
        for (int y = 0; y < composedDetailMap.Height; y++)
        {
            ReportRows(
                progress,
                BackgroundSuppressionStage.CombiningDetail,
                y,
                composedDetailMap.Height,
                0.82d,
                0.17d);
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * composedDetailMap.Width);
            for (int x = 0; x < composedDetailMap.Width; x++)
            {
                int index = rowOffset + x;
                double localSuppression = suppression[index];
                double reduced = composed[index] * (1d - localSuppression);
                effective[index] = (float)Math.Clamp(
                    Math.Max(settings.DetailFloor, reduced),
                    0d,
                    1d);
                artistic[index] = localSuppression > 0d
                    ? (float)-localSuppression
                    : effective[index];
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new BackgroundSuppressionProgress(
            BackgroundSuppressionStage.Completed,
            composedDetailMap.Height,
            composedDetailMap.Height,
            1d));
        return new BackgroundSuppressionResult(
            new ArtisticDetailField(composedDetailMap.Width, composedDetailMap.Height, artistic),
            new DetailMap(composedDetailMap.Width, composedDetailMap.Height, suppression),
            new DetailMap(composedDetailMap.Width, composedDetailMap.Height, protection),
            new DetailMap(composedDetailMap.Width, composedDetailMap.Height, effective));
    }

    private static void BuildProtection(
        ReadOnlySpan<float> automatic,
        ReadOnlySpan<float> composed,
        ReadOnlySpan<float> importance,
        ReadOnlySpan<float> subjects,
        ReadOnlySpan<float> silhouettes,
        ReadOnlySpan<float> uncertainty,
        ReadOnlySpan<float> subjectBoundaries,
        Span<float> protection,
        int width,
        int height,
        BackgroundSuppressionSettings settings,
        IProgress<BackgroundSuppressionProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (int y = 0; y < height; y++)
        {
            ReportRows(progress, BackgroundSuppressionStage.BuildingProtection, y, height, 0.02d, 0.28d);
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + x;
                double manualIncrease = Math.Max(0d, composed[index] - automatic[index]);
                double semanticProtection = Math.Max(subjects[index], importance[index] * 0.78d);
                double silhouetteProtection = Math.Max(silhouettes[index], subjectBoundaries[index])
                    * settings.SilhouetteProtection;
                double uncertaintyProtection = uncertainty[index] * settings.UncertaintyProtection;
                protection[index] = (float)Math.Clamp(
                    Math.Max(
                        manualIncrease,
                        Math.Max(semanticProtection, Math.Max(silhouetteProtection, uncertaintyProtection))),
                    0d,
                    1d);
            }
        }
    }

    private static void BuildSuppression(
        ReadOnlySpan<float> background,
        ReadOnlySpan<float> protection,
        Span<float> suppression,
        int width,
        int height,
        BackgroundSuppressionSettings settings,
        IProgress<BackgroundSuppressionProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (int y = 0; y < height; y++)
        {
            ReportRows(progress, BackgroundSuppressionStage.EstimatingSuppression, y, height, 0.30d, 0.30d);
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + x;
                suppression[index] = (float)Math.Clamp(
                    background[index]
                    * settings.OverallStrength
                    * (1d - protection[index]),
                    0d,
                    1d);
            }
        }
    }

    private static void ReapplyProtection(
        Span<float> suppression,
        ReadOnlySpan<float> protection,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + x;
                suppression[index] *= 1f - protection[index];
            }
        }
    }

    private static float[] Smooth(
        float[] source,
        int width,
        int height,
        int radius,
        IProgress<BackgroundSuppressionProgress>? progress,
        CancellationToken cancellationToken)
    {
        float[] horizontal = new float[source.Length];
        float[] output = new float[source.Length];
        for (int y = 0; y < height; y++)
        {
            ReportRows(progress, BackgroundSuppressionStage.SmoothingTransitions, y, height, 0.60d, 0.11d);
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                double total = 0d;
                int count = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = x + offset;
                    if (sampleX < 0 || sampleX >= width)
                    {
                        continue;
                    }

                    total += source[rowOffset + sampleX];
                    count++;
                }

                horizontal[rowOffset + x] = (float)(total / count);
            }
        }

        for (int y = 0; y < height; y++)
        {
            ReportRows(progress, BackgroundSuppressionStage.SmoothingTransitions, y, height, 0.71d, 0.11d);
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++)
            {
                double total = 0d;
                int count = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleY = y + offset;
                    if (sampleY < 0 || sampleY >= height)
                    {
                        continue;
                    }

                    total += horizontal[checked((sampleY * width) + x)];
                    count++;
                }

                output[checked((y * width) + x)] = (float)(total / count);
            }
        }

        return output;
    }

    private static void ReportRows(
        IProgress<BackgroundSuppressionProgress>? progress,
        BackgroundSuppressionStage stage,
        int completedRow,
        int totalRows,
        double start,
        double span)
    {
        if (progress is null || (completedRow % ProgressRowBatch != 0 && completedRow + 1 != totalRows))
        {
            return;
        }

        double rowFraction = totalRows == 0 ? 1d : completedRow / (double)totalRows;
        progress.Report(new BackgroundSuppressionProgress(
            stage,
            completedRow,
            totalRows,
            Math.Clamp(start + (rowFraction * span), 0d, 1d)));
    }
}
