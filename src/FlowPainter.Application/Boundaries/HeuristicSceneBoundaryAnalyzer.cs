using System.Diagnostics.CodeAnalysis;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Boundaries;

public sealed class HeuristicSceneBoundaryAnalyzer : ISceneBoundaryAnalyzer
{
    public const string ProviderIdentifier = "heuristic-scene-boundaries-v1";
    private const int ProgressRowBatch = 16;
    private const double MinimumSignal = 1e-12d;
    private const double MaximumColorTensorMagnitude = 1.224744871391589d;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The analyzer is an application service with instance semantics so model-backed boundary providers can replace it without changing call sites.")]
    public Task<SceneBoundaryAnalysisResult> AnalyzeAsync(
        IRgbaPixelSource source,
        SemanticAnalysisResult semanticAnalysis,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(semanticAnalysis);
        ArgumentNullException.ThrowIfNull(settings);

        if (source.Size != semanticAnalysis.ImportanceMap.Size)
        {
            throw new ArgumentException(
                "The source image and semantic maps must have identical dimensions.",
                nameof(semanticAnalysis));
        }

        return Task.Run(
            () => Analyze(source, semanticAnalysis, settings, progress, cancellationToken),
            cancellationToken);
    }

    private static SceneBoundaryAnalysisResult Analyze(
        IRgbaPixelSource source,
        SemanticAnalysisResult semanticAnalysis,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ImageSize size = source.Size;
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.Preparing,
            0,
            size.Height,
            0d));

        if (!settings.Enabled)
        {
            progress?.Report(new SceneBoundaryAnalysisProgress(
                SceneBoundaryAnalysisStage.Completed,
                size.Height,
                size.Height,
                1d));
            return SceneBoundaryAnalysisResult.CreateEmpty(size);
        }

        PixelChannels channels = ReadChannels(source, cancellationToken);
        int length = checked((int)size.PixelCount);
        double[] fineStrength = new double[length];
        double[] coarseStrength = new double[length];
        double[] multiscalePersistence = new double[length];
        BoundaryVector[] directions = new BoundaryVector[length];

        ComputeMultiscaleEdges(
            channels,
            size,
            settings,
            fineStrength,
            coarseStrength,
            multiscalePersistence,
            directions,
            progress,
            cancellationToken);

        progress?.Report(new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.LinkingContours,
            0,
            size.Height,
            0.48d));
        double[] continuity = ComputeContinuity(
            fineStrength,
            directions,
            size,
            progress,
            cancellationToken);

        progress?.Report(new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.ClassifyingBoundaries,
            0,
            size.Height,
            0.64d));
        ClassificationMaps maps = ClassifyBoundaries(
            fineStrength,
            coarseStrength,
            multiscalePersistence,
            continuity,
            semanticAnalysis,
            size,
            settings,
            progress,
            cancellationToken);

        progress?.Report(new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.EstimatingBackground,
            0,
            size.Height,
            0.80d));
        ComputeBackgroundAndUncertainty(
            maps,
            semanticAnalysis,
            size,
            settings,
            progress,
            cancellationToken);

        if (settings.SmoothingRadius > 0)
        {
            progress?.Report(new SceneBoundaryAnalysisProgress(
                SceneBoundaryAnalysisStage.SmoothingMaps,
                0,
                size.Height,
                0.90d));
            maps.EdgeImportance = BoxBlur(maps.EdgeImportance, size, settings.SmoothingRadius, cancellationToken);
            maps.InternalStructure = BoxBlur(maps.InternalStructure, size, settings.SmoothingRadius, cancellationToken);
            maps.TextureEdges = BoxBlur(maps.TextureEdges, size, settings.SmoothingRadius, cancellationToken);
            maps.BackgroundConfidence = BoxBlur(maps.BackgroundConfidence, size, settings.SmoothingRadius, cancellationToken);
            maps.Uncertainty = BoxBlur(maps.Uncertainty, size, settings.SmoothingRadius, cancellationToken);
        }

        progress?.Report(new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.Completed,
            size.Height,
            size.Height,
            1d));
        return new SceneBoundaryAnalysisResult(
            CreateMap(size, maps.EdgeStrength),
            CreateMap(size, maps.EdgeImportance),
            CreateMap(size, maps.SubjectBoundary),
            CreateMap(size, maps.InternalStructure),
            CreateMap(size, maps.TextureEdges),
            CreateMap(size, maps.BackgroundConfidence),
            CreateMap(size, maps.Uncertainty),
            new BoundaryDirectionField(size.Width, size.Height, directions),
            ProviderIdentifier);
    }

    private static PixelChannels ReadChannels(
        IRgbaPixelSource source,
        CancellationToken cancellationToken)
    {
        ImageSize size = source.Size;
        int length = checked((int)size.PixelCount);
        double[] red = new double[length];
        double[] green = new double[length];
        double[] blue = new double[length];
        double[] luminance = new double[length];

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double normalizedY = (y + 0.5d) / size.Height;
            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                Rgba32 color = source.SampleNearest(new NormalizedPoint(
                    (x + 0.5d) / size.Width,
                    normalizedY));
                double alpha = color.Alpha / (double)byte.MaxValue;
                red[index] = (color.Red / (double)byte.MaxValue) * alpha;
                green[index] = (color.Green / (double)byte.MaxValue) * alpha;
                blue[index] = (color.Blue / (double)byte.MaxValue) * alpha;
                luminance[index] = (0.2126d * red[index])
                    + (0.7152d * green[index])
                    + (0.0722d * blue[index]);
            }
        }

        return new PixelChannels(red, green, blue, luminance);
    }

    private static void ComputeMultiscaleEdges(
        PixelChannels channels,
        ImageSize size,
        SceneBoundaryAnalysisSettings settings,
        double[] fineStrength,
        double[] coarseStrength,
        double[] multiscalePersistence,
        BoundaryVector[] directions,
        IProgress<SceneBoundaryAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        double edgeWeightTotal = settings.LuminanceWeight + settings.ColorWeight;
        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new SceneBoundaryAnalysisProgress(
                    SceneBoundaryAnalysisStage.ComputingMultiscaleEdges,
                    y,
                    size.Height,
                    0.03d + (0.42d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                GradientTensor fine = CalculateGradientTensor(channels, size, x, y, 1);
                GradientTensor coarse = CalculateGradientTensor(
                    channels,
                    size,
                    x,
                    y,
                    settings.CoarseRadius);
                double fineSignal = WeightedStrength(fine, settings, edgeWeightTotal);
                double coarseSignal = WeightedStrength(coarse, settings, edgeWeightTotal);
                fineStrength[index] = SmoothThreshold(fineSignal, settings.EdgeThreshold);
                coarseStrength[index] = SmoothThreshold(coarseSignal, settings.EdgeThreshold);
                multiscalePersistence[index] = fineStrength[index] <= MinimumSignal
                    ? 0d
                    : Math.Clamp(coarseStrength[index] / fineStrength[index], 0d, 1d);

                GradientTensor combined = fine.AddScaled(coarse, settings.MultiscaleWeight);
                directions[index] = fineStrength[index] <= MinimumSignal
                    ? default
                    : CalculateTangent(combined);
            }
        }
    }

    private static GradientTensor CalculateGradientTensor(
        PixelChannels channels,
        ImageSize size,
        int x,
        int y,
        int radius)
    {
        ChannelGradient red = CalculateChannelGradient(channels.Red, size, x, y, radius);
        ChannelGradient green = CalculateChannelGradient(channels.Green, size, x, y, radius);
        ChannelGradient blue = CalculateChannelGradient(channels.Blue, size, x, y, radius);
        ChannelGradient luminance = CalculateChannelGradient(channels.Luminance, size, x, y, radius);
        double jxx = (red.X * red.X) + (green.X * green.X) + (blue.X * blue.X);
        double jyy = (red.Y * red.Y) + (green.Y * green.Y) + (blue.Y * blue.Y);
        double jxy = (red.X * red.Y) + (green.X * green.Y) + (blue.X * blue.Y);
        return new GradientTensor(
            luminance.X,
            luminance.Y,
            jxx,
            jyy,
            jxy);
    }

    private static ChannelGradient CalculateChannelGradient(
        double[] channel,
        ImageSize size,
        int x,
        int y,
        int radius)
    {
        double left = GetValue(channel, size, x - radius, y);
        double right = GetValue(channel, size, x + radius, y);
        double top = GetValue(channel, size, x, y - radius);
        double bottom = GetValue(channel, size, x, y + radius);
        return new ChannelGradient((right - left) * 0.5d, (bottom - top) * 0.5d);
    }

    private static double WeightedStrength(
        GradientTensor tensor,
        SceneBoundaryAnalysisSettings settings,
        double weightTotal)
    {
        double luminance = Math.Clamp(
            Math.Sqrt((tensor.LuminanceX * tensor.LuminanceX)
                + (tensor.LuminanceY * tensor.LuminanceY)),
            0d,
            1d);
        double color = Math.Clamp(
            Math.Sqrt(tensor.ColorJxx + tensor.ColorJyy) / MaximumColorTensorMagnitude,
            0d,
            1d);
        return weightTotal <= MinimumSignal
            ? 0d
            : ((settings.LuminanceWeight * luminance) + (settings.ColorWeight * color)) / weightTotal;
    }

    private static BoundaryVector CalculateTangent(GradientTensor tensor)
    {
        double colorAngle = 0.5d * Math.Atan2(
            2d * tensor.ColorJxy,
            tensor.ColorJxx - tensor.ColorJyy);
        double colorMagnitude = tensor.ColorJxx + tensor.ColorJyy;
        double luminanceMagnitude = (tensor.LuminanceX * tensor.LuminanceX)
            + (tensor.LuminanceY * tensor.LuminanceY);
        double normalAngle = colorMagnitude >= luminanceMagnitude && colorMagnitude > MinimumSignal
            ? colorAngle
            : Math.Atan2(tensor.LuminanceY, tensor.LuminanceX);
        return new BoundaryVector(-Math.Sin(normalAngle), Math.Cos(normalAngle));
    }

    private static double[] ComputeContinuity(
        double[] edgeStrength,
        BoundaryVector[] directions,
        ImageSize size,
        IProgress<SceneBoundaryAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        double[] continuity = new double[edgeStrength.Length];
        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new SceneBoundaryAnalysisProgress(
                    SceneBoundaryAnalysisStage.LinkingContours,
                    y,
                    size.Height,
                    0.48d + (0.13d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                BoundaryVector direction = directions[index];
                if (!direction.IsDefined || edgeStrength[index] <= MinimumSignal)
                {
                    continue;
                }

                int stepX = Math.Abs(direction.X) >= 0.35d ? Math.Sign(direction.X) : 0;
                int stepY = Math.Abs(direction.Y) >= 0.35d ? Math.Sign(direction.Y) : 0;
                if (stepX == 0 && stepY == 0)
                {
                    stepX = 1;
                }

                double previous = NeighborContinuity(
                    edgeStrength,
                    directions,
                    size,
                    x - stepX,
                    y - stepY,
                    direction);
                double next = NeighborContinuity(
                    edgeStrength,
                    directions,
                    size,
                    x + stepX,
                    y + stepY,
                    direction);
                continuity[index] = (previous + next) * 0.5d;
            }
        }

        return continuity;
    }

    private static double NeighborContinuity(
        double[] edgeStrength,
        BoundaryVector[] directions,
        ImageSize size,
        int x,
        int y,
        BoundaryVector direction)
    {
        if (x < 0 || x >= size.Width || y < 0 || y >= size.Height)
        {
            return 0d;
        }

        int index = checked((y * size.Width) + x);
        BoundaryVector neighbor = directions[index];
        if (!neighbor.IsDefined)
        {
            return 0d;
        }

        double alignment = Math.Abs(direction.Dot(neighbor));
        return edgeStrength[index] * alignment;
    }

    private static ClassificationMaps ClassifyBoundaries(
        double[] fineStrength,
        double[] coarseStrength,
        double[] multiscalePersistence,
        double[] continuity,
        SemanticAnalysisResult semanticAnalysis,
        ImageSize size,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        float[] subjectValues = semanticAnalysis.SubjectMap.CopyValues();
        float[] silhouetteValues = semanticAnalysis.SilhouetteMap.CopyValues();
        float[] importanceValues = semanticAnalysis.ImportanceMap.CopyValues();
        int length = fineStrength.Length;
        ClassificationMaps maps = new(length);
        double importanceDenominator = 1d
            + settings.MultiscaleWeight
            + settings.ContinuityWeight
            + settings.SemanticBoundaryWeight;

        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new SceneBoundaryAnalysisProgress(
                    SceneBoundaryAnalysisStage.ClassifyingBoundaries,
                    y,
                    size.Height,
                    0.64d + (0.13d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                double strength = fineStrength[index];
                double silhouette = silhouetteValues[index];
                double subject = subjectValues[index];
                double semanticImportance = importanceValues[index];
                double texture = Math.Clamp(
                    (fineStrength[index] - (0.72d * coarseStrength[index]))
                    * (1d - (0.75d * continuity[index]))
                    * (1d - silhouette)
                    * (1d - (0.45d * subject)),
                    0d,
                    1d);
                double importanceSignal = strength
                    * (1d
                        + (settings.MultiscaleWeight * multiscalePersistence[index])
                        + (settings.ContinuityWeight * continuity[index])
                        + (settings.SemanticBoundaryWeight * silhouette));
                importanceSignal /= importanceDenominator;
                importanceSignal *= 1d - (settings.TextureSuppression * texture);

                maps.EdgeStrength[index] = strength;
                maps.TextureEdges[index] = texture;
                maps.SubjectBoundary[index] = Math.Clamp(
                    Math.Sqrt(Math.Max(0d, strength * silhouette)),
                    0d,
                    1d);
                maps.InternalStructure[index] = Math.Clamp(
                    strength
                    * subject
                    * (1d - (0.85d * silhouette))
                    * (0.4d + (0.6d * semanticImportance)),
                    0d,
                    1d);
                maps.EdgeImportance[index] = SmoothThreshold(
                    Math.Clamp(importanceSignal, 0d, 1d),
                    settings.ImportantEdgeThreshold);
            }
        }

        return maps;
    }

    private static void ComputeBackgroundAndUncertainty(
        ClassificationMaps maps,
        SemanticAnalysisResult semanticAnalysis,
        ImageSize size,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        float[] subject = semanticAnalysis.SubjectMap.CopyValues();
        float[] saliency = semanticAnalysis.SaliencyMap.CopyValues();
        float[] focal = semanticAnalysis.FocalMap.CopyValues();
        int[] distance = CalculateSubjectDistance(subject, size, cancellationToken);
        bool hasSubject = distance.Any(value => value == 0);
        double protectionDistance = settings.BoundaryProtectionRadius + 1d;

        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new SceneBoundaryAnalysisProgress(
                    SceneBoundaryAnalysisStage.EstimatingBackground,
                    y,
                    size.Height,
                    0.80d + (0.08d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                double distanceFactor = !hasSubject
                    ? 1d
                    : Math.Clamp(distance[index] / protectionDistance, 0d, 1d);
                double subjectConfidence = Math.Max(
                    subject[index],
                    Math.Max(maps.SubjectBoundary[index], focal[index]));
                double lowImportance = (1d - subject[index])
                    * (1d - focal[index])
                    * (1d - (0.75d * saliency[index]));
                double structuralFreedom = 0.65d
                    + (0.35d * (1d - maps.EdgeImportance[index]));
                double background = Math.Clamp(
                    lowImportance * distanceFactor * structuralFreedom,
                    0d,
                    1d);
                double classifiedConfidence = Math.Max(
                    subjectConfidence,
                    Math.Max(background, 0.85d * maps.SubjectBoundary[index]));

                maps.BackgroundConfidence[index] = background;
                maps.Uncertainty[index] = Math.Clamp(1d - classifiedConfidence, 0d, 1d);
            }
        }
    }

    private static int[] CalculateSubjectDistance(
        float[] subject,
        ImageSize size,
        CancellationToken cancellationToken)
    {
        int[] distance = new int[subject.Length];
        Array.Fill(distance, int.MaxValue);
        int[] queue = new int[subject.Length];
        int queueStart = 0;
        int queueEnd = 0;

        for (int index = 0; index < subject.Length; index++)
        {
            if (subject[index] >= 0.25f)
            {
                distance[index] = 0;
                queue[queueEnd++] = index;
            }
        }

        while (queueStart < queueEnd)
        {
            if ((queueStart & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int current = queue[queueStart++];
            int y = current / size.Width;
            int x = current - (y * size.Width);
            int nextDistance = distance[current] + 1;
            EnqueueDistance(x - 1, y, nextDistance, size, distance, queue, ref queueEnd);
            EnqueueDistance(x + 1, y, nextDistance, size, distance, queue, ref queueEnd);
            EnqueueDistance(x, y - 1, nextDistance, size, distance, queue, ref queueEnd);
            EnqueueDistance(x, y + 1, nextDistance, size, distance, queue, ref queueEnd);
        }

        if (queueEnd == 0)
        {
            Array.Fill(distance, Math.Max(size.Width, size.Height));
        }

        return distance;
    }

    private static void EnqueueDistance(
        int x,
        int y,
        int value,
        ImageSize size,
        int[] distance,
        int[] queue,
        ref int queueEnd)
    {
        if (x < 0 || x >= size.Width || y < 0 || y >= size.Height)
        {
            return;
        }

        int index = checked((y * size.Width) + x);
        if (value >= distance[index])
        {
            return;
        }

        distance[index] = value;
        queue[queueEnd++] = index;
    }

    private static double GetValue(
        double[] values,
        ImageSize size,
        int x,
        int y)
    {
        int clampedX = Math.Clamp(x, 0, size.Width - 1);
        int clampedY = Math.Clamp(y, 0, size.Height - 1);
        return values[checked((clampedY * size.Width) + clampedX)];
    }

    private static double SmoothThreshold(double value, double threshold)
    {
        if (threshold >= 1d)
        {
            return value >= 1d ? 1d : 0d;
        }

        double normalized = Math.Clamp((value - threshold) / (1d - threshold), 0d, 1d);
        return normalized * normalized * (3d - (2d * normalized));
    }

    private static double[] BoxBlur(
        double[] source,
        ImageSize size,
        int radius,
        CancellationToken cancellationToken)
    {
        double[] horizontal = new double[source.Length];
        double[] result = new double[source.Length];

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * size.Width);
            double sum = 0d;
            int initialEnd = Math.Min(size.Width - 1, radius);
            for (int x = 0; x <= initialEnd; x++)
            {
                sum += source[rowOffset + x];
            }

            for (int x = 0; x < size.Width; x++)
            {
                int currentStart = Math.Max(0, x - radius);
                int currentEnd = Math.Min(size.Width - 1, x + radius);
                horizontal[rowOffset + x] = sum / (currentEnd - currentStart + 1);
                int removeIndex = x - radius;
                int addIndex = x + radius + 1;
                if (removeIndex >= 0)
                {
                    sum -= source[rowOffset + removeIndex];
                }

                if (addIndex < size.Width)
                {
                    sum += source[rowOffset + addIndex];
                }
            }
        }

        double[] columnSums = new double[size.Width];
        int initialBottom = Math.Min(size.Height - 1, radius);
        for (int y = 0; y <= initialBottom; y++)
        {
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                columnSums[x] += horizontal[rowOffset + x];
            }
        }

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int start = Math.Max(0, y - radius);
            int end = Math.Min(size.Height - 1, y + radius);
            int count = end - start + 1;
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                result[rowOffset + x] = Math.Clamp(columnSums[x] / count, 0d, 1d);
            }

            int removeRow = y - radius;
            int addRow = y + radius + 1;
            if (removeRow >= 0)
            {
                int removeOffset = checked(removeRow * size.Width);
                for (int x = 0; x < size.Width; x++)
                {
                    columnSums[x] -= horizontal[removeOffset + x];
                }
            }

            if (addRow < size.Height)
            {
                int addOffset = checked(addRow * size.Width);
                for (int x = 0; x < size.Width; x++)
                {
                    columnSums[x] += horizontal[addOffset + x];
                }
            }
        }

        return result;
    }

    private static DetailMap CreateMap(ImageSize size, double[] values)
    {
        float[] normalized = new float[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            normalized[index] = (float)Math.Clamp(values[index], 0d, 1d);
        }

        return new DetailMap(size.Width, size.Height, normalized);
    }

    private sealed record PixelChannels(
        double[] Red,
        double[] Green,
        double[] Blue,
        double[] Luminance);

    private readonly record struct ChannelGradient(double X, double Y);

    private readonly record struct GradientTensor(
        double LuminanceX,
        double LuminanceY,
        double ColorJxx,
        double ColorJyy,
        double ColorJxy)
    {
        public GradientTensor AddScaled(GradientTensor other, double scale)
        {
            return new GradientTensor(
                LuminanceX + (other.LuminanceX * scale),
                LuminanceY + (other.LuminanceY * scale),
                ColorJxx + (other.ColorJxx * scale),
                ColorJyy + (other.ColorJyy * scale),
                ColorJxy + (other.ColorJxy * scale));
        }
    }

    private sealed class ClassificationMaps
    {
        public ClassificationMaps(int length)
        {
            EdgeStrength = new double[length];
            EdgeImportance = new double[length];
            SubjectBoundary = new double[length];
            InternalStructure = new double[length];
            TextureEdges = new double[length];
            BackgroundConfidence = new double[length];
            Uncertainty = new double[length];
        }

        public double[] EdgeStrength { get; }

        public double[] EdgeImportance { get; set; }

        public double[] SubjectBoundary { get; }

        public double[] InternalStructure { get; set; }

        public double[] TextureEdges { get; set; }

        public double[] BackgroundConfidence { get; set; }

        public double[] Uncertainty { get; set; }
    }
}
