using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Semantics;

public sealed class HeuristicSemanticImportanceAnalyzer : ISemanticImportanceAnalyzer
{
    public const string ProviderIdentifier = "heuristic-saliency-v1";
    private const int ProgressRowBatch = 16;
    private const double MaximumRgbDistance = 1.7320508075688772d;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The analyzer is an application service with instance semantics so local ML-backed implementations can replace it without changing call sites.")]
    public Task<SemanticAnalysisResult> AnalyzeAsync(
        IRgbaPixelSource source,
        SemanticAnalysisSettings settings,
        IProgress<SemanticAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);

        return Task.Run(
            () => Analyze(source, settings, progress, cancellationToken),
            cancellationToken);
    }

    private static SemanticAnalysisResult Analyze(
        IRgbaPixelSource source,
        SemanticAnalysisSettings settings,
        IProgress<SemanticAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ImageSize size = source.Size;
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new SemanticAnalysisProgress(
            SemanticAnalysisStage.Preparing,
            0,
            size.Height,
            0d));

        if (!settings.Enabled)
        {
            progress?.Report(new SemanticAnalysisProgress(
                SemanticAnalysisStage.Completed,
                size.Height,
                size.Height,
                1d));
            return SemanticAnalysisResult.CreateEmpty(size);
        }

        PixelChannels channels = ReadChannels(source, cancellationToken);
        double[] saliency = ComputeSaliency(
            channels,
            size,
            settings,
            progress,
            cancellationToken);
        NormalizeInPlace(saliency);
        if (settings.SmoothingRadius > 0)
        {
            saliency = BoxBlur(saliency, size, settings.SmoothingRadius, cancellationToken);
            NormalizeInPlace(saliency);
        }

        progress?.Report(new SemanticAnalysisProgress(
            SemanticAnalysisStage.SegmentingSubjects,
            0,
            size.Height,
            0.58d));
        bool[] subjectCandidates = CreateCandidateMask(saliency, settings.SubjectThreshold);
        if (subjectCandidates.Any(value => value))
        {
            subjectCandidates = Erode(
                Dilate(subjectCandidates, size, cancellationToken),
                size,
                cancellationToken);
        }

        ComponentSegmentation segmentation = SegmentComponents(
            subjectCandidates,
            saliency,
            size,
            settings,
            cancellationToken);
        BuildSemanticMaps(
            segmentation,
            saliency,
            size,
            settings,
            progress,
            cancellationToken,
            out double[] subjectMap,
            out double[] silhouetteMap,
            out double[] focalMap,
            out List<SemanticRegion> regions);

        progress?.Report(new SemanticAnalysisProgress(
            SemanticAnalysisStage.CombiningMaps,
            size.Height,
            size.Height,
            0.92d));
        double[] importanceMap = CombineMaps(
            saliency,
            subjectMap,
            silhouetteMap,
            focalMap,
            settings,
            cancellationToken);

        progress?.Report(new SemanticAnalysisProgress(
            SemanticAnalysisStage.Completed,
            size.Height,
            size.Height,
            1d));
        return new SemanticAnalysisResult(
            CreateMap(size, saliency),
            CreateMap(size, subjectMap),
            CreateMap(size, silhouetteMap),
            CreateMap(size, focalMap),
            CreateMap(size, importanceMap),
            regions,
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

    private static double[] ComputeSaliency(
        PixelChannels channels,
        ImageSize size,
        SemanticAnalysisSettings settings,
        IProgress<SemanticAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        double averageRed = channels.Red.Average();
        double averageGreen = channels.Green.Average();
        double averageBlue = channels.Blue.Average();
        IntegralChannels integral = new(channels, size);
        int localRadius = Math.Clamp(Math.Min(size.Width, size.Height) / 18, 2, 18);
        double[] saliency = new double[channels.Red.Length];

        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new SemanticAnalysisProgress(
                    SemanticAnalysisStage.ComputingSaliency,
                    y,
                    size.Height,
                    0.04d + (0.5d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                RgbVector localAverage = integral.GetAverage(x, y, localRadius);
                double globalContrast = ColorDistance(
                    channels.Red[index],
                    channels.Green[index],
                    channels.Blue[index],
                    averageRed,
                    averageGreen,
                    averageBlue);
                double localContrast = ColorDistance(
                    channels.Red[index],
                    channels.Green[index],
                    channels.Blue[index],
                    localAverage.Red,
                    localAverage.Green,
                    localAverage.Blue);
                double edge = CalculateEdge(channels.Luminance, size, x, y);
                double centerWeight = CalculateCenterWeight(size, x, y);
                double signal = (0.45d * globalContrast)
                    + (0.35d * localContrast)
                    + (0.20d * edge);
                saliency[index] = signal * (1d + (settings.CenterBias * centerWeight));
            }
        }

        return saliency;
    }

    private static bool[] CreateCandidateMask(
        double[] saliency,
        double threshold)
    {
        bool[] result = new bool[saliency.Length];
        for (int index = 0; index < saliency.Length; index++)
        {
            result[index] = saliency[index] >= threshold;
        }

        return result;
    }

    private static ComponentSegmentation SegmentComponents(
        bool[] mask,
        double[] saliency,
        ImageSize size,
        SemanticAnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        int[] labels = new int[mask.Length];
        Array.Fill(labels, -1);
        int[] queue = new int[mask.Length];
        List<Component> components = [];
        int minimumArea = Math.Max(
            1,
            checked((int)Math.Ceiling(size.PixelCount * settings.MinimumSubjectAreaRatio)));

        for (int index = 0; index < mask.Length; index++)
        {
            if (!mask[index] || labels[index] >= 0)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            int label = components.Count;
            int queueStart = 0;
            int queueEnd = 0;
            queue[queueEnd++] = index;
            labels[index] = label;
            int area = 0;
            int minimumX = size.Width;
            int minimumY = size.Height;
            int maximumX = 0;
            int maximumY = 0;
            double saliencySum = 0d;
            double peakSaliency = -1d;
            int peakIndex = index;

            while (queueStart < queueEnd)
            {
                int current = queue[queueStart++];
                int y = current / size.Width;
                int x = current - (y * size.Width);
                area++;
                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
                double currentSaliency = saliency[current];
                saliencySum += currentSaliency;
                if (currentSaliency > peakSaliency)
                {
                    peakSaliency = currentSaliency;
                    peakIndex = current;
                }

                EnqueueNeighbor(x - 1, y, label, mask, labels, queue, ref queueEnd, size);
                EnqueueNeighbor(x + 1, y, label, mask, labels, queue, ref queueEnd, size);
                EnqueueNeighbor(x, y - 1, label, mask, labels, queue, ref queueEnd, size);
                EnqueueNeighbor(x, y + 1, label, mask, labels, queue, ref queueEnd, size);
            }

            components.Add(new Component(
                label,
                area,
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                saliencySum,
                peakIndex,
                area >= minimumArea));
        }

        List<Component> selected = components
            .Where(component => component.IsLargeEnough)
            .OrderByDescending(component => CalculateComponentScore(component, size, settings.CenterBias))
            .ThenBy(component => component.Label)
            .Take(settings.MaximumSubjects)
            .ToList();
        return new ComponentSegmentation(labels, selected);
    }

    private static void BuildSemanticMaps(
        ComponentSegmentation segmentation,
        double[] saliency,
        ImageSize size,
        SemanticAnalysisSettings settings,
        IProgress<SemanticAnalysisProgress>? progress,
        CancellationToken cancellationToken,
        out double[] subjectMap,
        out double[] silhouetteMap,
        out double[] focalMap,
        out List<SemanticRegion> regions)
    {
        subjectMap = new double[saliency.Length];
        silhouetteMap = new double[saliency.Length];
        focalMap = new double[saliency.Length];
        regions = [];
        HashSet<int> selectedLabels = segmentation.Components
            .Select(component => component.Label)
            .ToHashSet();

        progress?.Report(new SemanticAnalysisProgress(
            SemanticAnalysisStage.BuildingSilhouettes,
            0,
            size.Height,
            0.72d));

        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new SemanticAnalysisProgress(
                    SemanticAnalysisStage.BuildingSilhouettes,
                    y,
                    size.Height,
                    0.72d + (0.16d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                int label = segmentation.Labels[index];
                if (!selectedLabels.Contains(label))
                {
                    continue;
                }

                subjectMap[index] = Math.Max(0.55d, saliency[index]);
                if (IsBoundaryPixel(segmentation.Labels, size, x, y, label))
                {
                    silhouetteMap[index] = 1d;
                }
            }
        }

        if (settings.BoundaryRadius > 0)
        {
            silhouetteMap = BoxBlur(
                silhouetteMap,
                size,
                settings.BoundaryRadius,
                cancellationToken);
            NormalizeInPlace(silhouetteMap);
        }

        for (int rank = 0; rank < segmentation.Components.Count; rank++)
        {
            Component component = segmentation.Components[rank];
            double score = CalculateComponentScore(component, size, settings.CenterBias);
            double confidence = Math.Clamp(score, 0d, 1d);
            string subjectId = $"semantic-subject-{rank + 1:D2}";
            regions.Add(new SemanticRegion(
                subjectId,
                component.GetBounds(size),
                confidence,
                Math.Clamp(0.65d + (0.35d * confidence), 0d, 1d),
                SemanticRegionRole.Subject,
                SemanticSubjectKind.Unknown,
                $"Subject {rank + 1}",
                ProviderIdentifier));

            int peakY = component.PeakIndex / size.Width;
            int peakX = component.PeakIndex - (peakY * size.Width);
            int componentWidth = component.MaximumX - component.MinimumX + 1;
            int componentHeight = component.MaximumY - component.MinimumY + 1;
            int focalRadius = Math.Clamp(
                Math.Max(2, Math.Min(componentWidth, componentHeight) / 8),
                2,
                Math.Max(2, Math.Min(size.Width, size.Height) / 6));
            PaintGaussian(focalMap, size, peakX, peakY, focalRadius);
            regions.Add(new SemanticRegion(
                $"semantic-focus-{rank + 1:D2}",
                CreateFocalBounds(size, peakX, peakY, focalRadius),
                confidence,
                Math.Clamp(0.8d + (0.2d * confidence), 0d, 1d),
                rank == 0 ? SemanticRegionRole.CriticalDetail : SemanticRegionRole.FocalArea,
                SemanticSubjectKind.Detail,
                rank == 0 ? "Primary focal point" : $"Focal point {rank + 1}",
                ProviderIdentifier));
        }

        NormalizeInPlace(focalMap);
    }

    private static double[] CombineMaps(
        double[] saliency,
        double[] subject,
        double[] silhouette,
        double[] focal,
        SemanticAnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        double totalWeight = settings.SaliencyWeight
            + settings.SubjectWeight
            + settings.SilhouetteWeight
            + settings.FocalWeight;
        double[] result = new double[saliency.Length];
        if (totalWeight <= 0d)
        {
            return result;
        }

        for (int index = 0; index < result.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            result[index] = Math.Clamp(
                ((saliency[index] * settings.SaliencyWeight)
                    + (subject[index] * settings.SubjectWeight)
                    + (silhouette[index] * settings.SilhouetteWeight)
                    + (focal[index] * settings.FocalWeight))
                / totalWeight,
                0d,
                1d);
        }

        NormalizeInPlace(result);
        return result;
    }

    private static DetailMap CreateMap(ImageSize size, double[] values)
    {
        float[] converted = new float[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            converted[index] = (float)Math.Clamp(values[index], 0d, 1d);
        }

        return new DetailMap(size.Width, size.Height, converted);
    }

    private static double CalculateEdge(
        double[] luminance,
        ImageSize size,
        int x,
        int y)
    {
        double left = GetValue(luminance, size, x - 1, y);
        double right = GetValue(luminance, size, x + 1, y);
        double top = GetValue(luminance, size, x, y - 1);
        double bottom = GetValue(luminance, size, x, y + 1);
        double horizontal = (right - left) * 0.5d;
        double vertical = (bottom - top) * 0.5d;
        return Math.Clamp(Math.Sqrt((horizontal * horizontal) + (vertical * vertical)), 0d, 1d);
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

    private static double ColorDistance(
        double firstRed,
        double firstGreen,
        double firstBlue,
        double secondRed,
        double secondGreen,
        double secondBlue)
    {
        double red = firstRed - secondRed;
        double green = firstGreen - secondGreen;
        double blue = firstBlue - secondBlue;
        return Math.Sqrt((red * red) + (green * green) + (blue * blue)) / MaximumRgbDistance;
    }

    private static double CalculateCenterWeight(ImageSize size, int x, int y)
    {
        double normalizedX = ((x + 0.5d) / size.Width) - 0.5d;
        double normalizedY = ((y + 0.5d) / size.Height) - 0.5d;
        double distance = Math.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
        return Math.Clamp(1d - (distance / 0.7071067811865476d), 0d, 1d);
    }

    private static void NormalizeInPlace(double[] values)
    {
        if (values.Length == 0)
        {
            return;
        }

        double minimum = values.Min();
        double maximum = values.Max();
        double range = maximum - minimum;
        if (range <= 1e-12d)
        {
            Array.Clear(values);
            return;
        }

        for (int index = 0; index < values.Length; index++)
        {
            values[index] = Math.Clamp((values[index] - minimum) / range, 0d, 1d);
        }
    }

    private static bool[] Dilate(
        bool[] source,
        ImageSize size,
        CancellationToken cancellationToken)
    {
        bool[] result = new bool[source.Length];
        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < size.Width; x++)
            {
                bool active = false;
                for (int offsetY = -1; offsetY <= 1 && !active; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int sampleX = x + offsetX;
                        int sampleY = y + offsetY;
                        if (sampleX >= 0
                            && sampleX < size.Width
                            && sampleY >= 0
                            && sampleY < size.Height
                            && source[checked((sampleY * size.Width) + sampleX)])
                        {
                            active = true;
                            break;
                        }
                    }
                }

                result[checked((y * size.Width) + x)] = active;
            }
        }

        return result;
    }

    private static bool[] Erode(
        bool[] source,
        ImageSize size,
        CancellationToken cancellationToken)
    {
        bool[] result = new bool[source.Length];
        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < size.Width; x++)
            {
                bool active = true;
                for (int offsetY = -1; offsetY <= 1 && active; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int sampleX = x + offsetX;
                        int sampleY = y + offsetY;
                        if (sampleX < 0
                            || sampleX >= size.Width
                            || sampleY < 0
                            || sampleY >= size.Height
                            || !source[checked((sampleY * size.Width) + sampleX)])
                        {
                            active = false;
                            break;
                        }
                    }
                }

                result[checked((y * size.Width) + x)] = active;
            }
        }

        return result;
    }

    private static void EnqueueNeighbor(
        int x,
        int y,
        int label,
        bool[] mask,
        int[] labels,
        int[] queue,
        ref int queueEnd,
        ImageSize size)
    {
        if (x < 0 || x >= size.Width || y < 0 || y >= size.Height)
        {
            return;
        }

        int index = checked((y * size.Width) + x);
        if (!mask[index] || labels[index] >= 0)
        {
            return;
        }

        labels[index] = label;
        queue[queueEnd++] = index;
    }

    private static double CalculateComponentScore(
        Component component,
        ImageSize size,
        double centerBias)
    {
        double meanSaliency = component.SaliencySum / component.Area;
        double areaRatio = component.Area / (double)size.PixelCount;
        double centerX = (component.MinimumX + component.MaximumX + 1d) * 0.5d / size.Width;
        double centerY = (component.MinimumY + component.MaximumY + 1d) * 0.5d / size.Height;
        double distance = Math.Sqrt(
            ((centerX - 0.5d) * (centerX - 0.5d))
            + ((centerY - 0.5d) * (centerY - 0.5d)));
        double center = Math.Clamp(1d - (distance / 0.7071067811865476d), 0d, 1d);
        double areaFactor = Math.Clamp(Math.Sqrt(areaRatio) * 3d, 0d, 1d);
        return Math.Clamp(
            meanSaliency * (0.65d + (0.35d * areaFactor)) * (1d + (centerBias * center * 0.25d)),
            0d,
            1d);
    }

    private static bool IsBoundaryPixel(
        int[] labels,
        ImageSize size,
        int x,
        int y,
        int label)
    {
        return GetLabel(labels, size, x - 1, y) != label
            || GetLabel(labels, size, x + 1, y) != label
            || GetLabel(labels, size, x, y - 1) != label
            || GetLabel(labels, size, x, y + 1) != label;
    }

    private static int GetLabel(
        int[] labels,
        ImageSize size,
        int x,
        int y)
    {
        if (x < 0 || x >= size.Width || y < 0 || y >= size.Height)
        {
            return -1;
        }

        return labels[checked((y * size.Width) + x)];
    }

    private static void PaintGaussian(
        double[] map,
        ImageSize size,
        int centerX,
        int centerY,
        int radius)
    {
        double sigma = Math.Max(1d, radius * 0.5d);
        double denominator = 2d * sigma * sigma;
        int minimumX = Math.Max(0, centerX - radius);
        int maximumX = Math.Min(size.Width - 1, centerX + radius);
        int minimumY = Math.Max(0, centerY - radius);
        int maximumY = Math.Min(size.Height - 1, centerY + radius);

        for (int y = minimumY; y <= maximumY; y++)
        {
            for (int x = minimumX; x <= maximumX; x++)
            {
                int deltaX = x - centerX;
                int deltaY = y - centerY;
                double value = Math.Exp(-((deltaX * deltaX) + (deltaY * deltaY)) / denominator);
                int index = checked((y * size.Width) + x);
                map[index] = Math.Max(map[index], value);
            }
        }
    }

    private static NormalizedRect CreateFocalBounds(
        ImageSize size,
        int centerX,
        int centerY,
        int radius)
    {
        double left = Math.Max(0d, (centerX - radius) / (double)size.Width);
        double top = Math.Max(0d, (centerY - radius) / (double)size.Height);
        double right = Math.Min(1d, (centerX + radius + 1d) / size.Width);
        double bottom = Math.Min(1d, (centerY + radius + 1d) / size.Height);
        return new NormalizedRect(left, top, right, bottom);
    }

    private static double[] BoxBlur(
        double[] source,
        ImageSize size,
        int radius,
        CancellationToken cancellationToken)
    {
        if (radius <= 0)
        {
            return (double[])source.Clone();
        }

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
                int start = Math.Max(0, x - radius);
                int end = Math.Min(size.Width - 1, x + radius);
                horizontal[rowOffset + x] = sum / (end - start + 1);

                int remove = x - radius;
                int add = x + radius + 1;
                if (remove >= 0)
                {
                    sum -= source[rowOffset + remove];
                }

                if (add < size.Width)
                {
                    sum += source[rowOffset + add];
                }
            }
        }

        double[] columnSums = new double[size.Width];
        int firstEnd = Math.Min(size.Height - 1, radius);
        for (int y = 0; y <= firstEnd; y++)
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
            int rowOffset = checked(y * size.Width);
            int count = end - start + 1;
            for (int x = 0; x < size.Width; x++)
            {
                result[rowOffset + x] = columnSums[x] / count;
            }

            int remove = y - radius;
            int add = y + radius + 1;
            if (remove >= 0)
            {
                int removeOffset = checked(remove * size.Width);
                for (int x = 0; x < size.Width; x++)
                {
                    columnSums[x] -= horizontal[removeOffset + x];
                }
            }

            if (add < size.Height)
            {
                int addOffset = checked(add * size.Width);
                for (int x = 0; x < size.Width; x++)
                {
                    columnSums[x] += horizontal[addOffset + x];
                }
            }
        }

        return result;
    }

    private readonly record struct RgbVector(double Red, double Green, double Blue);

    private sealed record PixelChannels(
        double[] Red,
        double[] Green,
        double[] Blue,
        double[] Luminance);

    private sealed class IntegralChannels
    {
        private readonly int _stride;
        private readonly double[] _red;
        private readonly double[] _green;
        private readonly double[] _blue;

        public IntegralChannels(PixelChannels channels, ImageSize size)
        {
            _stride = size.Width + 1;
            int length = checked((size.Width + 1) * (size.Height + 1));
            _red = new double[length];
            _green = new double[length];
            _blue = new double[length];

            for (int y = 1; y <= size.Height; y++)
            {
                double rowRed = 0d;
                double rowGreen = 0d;
                double rowBlue = 0d;
                int sourceOffset = checked((y - 1) * size.Width);
                int integralOffset = checked(y * _stride);
                int previousOffset = checked((y - 1) * _stride);
                for (int x = 1; x <= size.Width; x++)
                {
                    int sourceIndex = sourceOffset + x - 1;
                    rowRed += channels.Red[sourceIndex];
                    rowGreen += channels.Green[sourceIndex];
                    rowBlue += channels.Blue[sourceIndex];
                    _red[integralOffset + x] = _red[previousOffset + x] + rowRed;
                    _green[integralOffset + x] = _green[previousOffset + x] + rowGreen;
                    _blue[integralOffset + x] = _blue[previousOffset + x] + rowBlue;
                }
            }
        }

        public RgbVector GetAverage(int x, int y, int radius)
        {
            int minimumX = Math.Max(0, x - radius);
            int minimumY = Math.Max(0, y - radius);
            int maximumX = Math.Min(_stride - 2, x + radius);
            int maximumY = Math.Min((_red.Length / _stride) - 2, y + radius);
            int count = checked((maximumX - minimumX + 1) * (maximumY - minimumY + 1));
            return new RgbVector(
                Sum(_red, minimumX, minimumY, maximumX, maximumY) / count,
                Sum(_green, minimumX, minimumY, maximumX, maximumY) / count,
                Sum(_blue, minimumX, minimumY, maximumX, maximumY) / count);
        }

        private double Sum(
            double[] integral,
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY)
        {
            int left = minimumX;
            int top = minimumY;
            int right = maximumX + 1;
            int bottom = maximumY + 1;
            return integral[checked((bottom * _stride) + right)]
                - integral[checked((top * _stride) + right)]
                - integral[checked((bottom * _stride) + left)]
                + integral[checked((top * _stride) + left)];
        }
    }

    private sealed record Component(
        int Label,
        int Area,
        int MinimumX,
        int MinimumY,
        int MaximumX,
        int MaximumY,
        double SaliencySum,
        int PeakIndex,
        bool IsLargeEnough)
    {
        public NormalizedRect GetBounds(ImageSize size)
        {
            return new NormalizedRect(
                MinimumX / (double)size.Width,
                MinimumY / (double)size.Height,
                (MaximumX + 1d) / size.Width,
                (MaximumY + 1d) / size.Height);
        }
    }

    private sealed record ComponentSegmentation(
        int[] Labels,
        List<Component> Components);
}
