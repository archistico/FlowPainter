using System.Diagnostics.CodeAnalysis;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public sealed class SlicRegionSegmentationAnalyzer : IRegionSegmentationAnalyzer
{
    private const int ProgressRowBatch = 16;
    private const double PreparingEnd = 0.03d;
    private const double SmoothingEnd = 0.15d;
    private const double ColorConversionEnd = 0.30d;
    private const double InitializationEnd = 0.35d;
    private const double AssignmentEnd = 0.90d;
    private const double UpdateEnd = 0.92d;
    private const double ConnectivityEnd = 0.95d;
    private const double DescriptorEnd = 0.975d;
    private const double AdjacencyEnd = 0.995d;
    private const double HierarchyEnd = 0.999d;
    private const double MinimumComponentAreaFraction = 0.25d;

    [SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "The analyzer is an application service with instance semantics so decorators or alternate deterministic implementations can replace it without changing call sites.")]
    public Task<RegionSegmentationResult> AnalyzeAsync(
        RegionSegmentationRequest request,
        IProgress<RegionSegmentationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.Run(
            () => Analyze(request, progress, cancellationToken),
            cancellationToken);
    }

    private static RegionSegmentationResult Analyze(
        RegionSegmentationRequest request,
        IProgress<RegionSegmentationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ImageSize size = request.Source.Size;
        RegionSegmentationSettings settings = request.Settings;
        RegionSegmentationEstimate estimate = RegionSegmentationEstimator.Estimate(size, settings);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            estimate.EstimatedPeakBytes,
            "SLIC regional segmentation");

        cancellationToken.ThrowIfCancellationRequested();
        Report(
            progress,
            RegionSegmentationStage.Preparing,
            0d,
            0d,
            0,
            settings.MaximumIterations);

        if (!settings.Enabled)
        {
            return CreateSingleRegionResult(request, progress, cancellationToken);
        }

        SlicColor[] colors = ReadSource(
            request.Source,
            settings,
            progress,
            cancellationToken);
        ConvertToLab(colors, size, settings.MaximumIterations, progress, cancellationToken);

        GridGeometry grid = GridGeometry.Create(size, settings.TargetRegionSize);
        SlicCluster[] clusters = InitializeClusters(
            colors,
            size,
            grid,
            settings.MaximumIterations,
            progress,
            cancellationToken);
        int[] assignments = new int[checked((int)size.PixelCount)];
        float[] distances = new float[assignments.Length];
        ClusterAccumulator[] accumulators = new ClusterAccumulator[clusters.Length];

        bool converged = false;
        double maximumDisplacement = 0d;
        int completedIterations = 0;

        for (int iteration = 1; iteration <= settings.MaximumIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double iterationStartFraction = (iteration - 1d) / settings.MaximumIterations;
            Report(
                progress,
                RegionSegmentationStage.AssigningPixels,
                iterationStartFraction,
                Interpolate(InitializationEnd, AssignmentEnd, iterationStartFraction),
                iteration - 1,
                settings.MaximumIterations);

            AssignPixels(
                colors,
                size,
                clusters,
                grid,
                settings.Compactness,
                assignments,
                distances,
                cancellationToken);

            maximumDisplacement = UpdateClusters(
                colors,
                size,
                clusters,
                assignments,
                accumulators,
                cancellationToken);
            completedIterations = iteration;

            double iterationEndFraction = iteration / (double)settings.MaximumIterations;
            Report(
                progress,
                RegionSegmentationStage.AssigningPixels,
                iterationEndFraction,
                Interpolate(InitializationEnd, AssignmentEnd, iterationEndFraction),
                iteration,
                settings.MaximumIterations);

            if (maximumDisplacement <= settings.ConvergenceTolerance)
            {
                converged = true;
                break;
            }
        }

        Report(
            progress,
            RegionSegmentationStage.UpdatingClusters,
            1d,
            UpdateEnd,
            completedIterations,
            settings.MaximumIterations);

        cancellationToken.ThrowIfCancellationRequested();
        Report(
            progress,
            RegionSegmentationStage.RepairingConnectivity,
            0d,
            UpdateEnd,
            completedIterations,
            settings.MaximumIterations);

        int minimumRegionPixelCount = CalculateMinimumRegionPixelCount(size, clusters.Length);
        RegionConnectivityResult connectivity = RegionConnectivityNormalizer.Normalize(
            size,
            clusters.Length,
            assignments,
            minimumRegionPixelCount,
            cancellationToken);
        RegionLabelMap labels = connectivity.Labels;

        Report(
            progress,
            RegionSegmentationStage.RepairingConnectivity,
            1d,
            ConnectivityEnd,
            completedIterations,
            settings.MaximumIterations);
        Report(
            progress,
            RegionSegmentationStage.BuildingResult,
            0d,
            ConnectivityEnd,
            completedIterations,
            settings.MaximumIterations);

        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(
            request.Source,
            labels,
            cancellationToken);
        Report(
            progress,
            RegionSegmentationStage.BuildingResult,
            1d,
            DescriptorEnd,
            completedIterations,
            settings.MaximumIterations);
        Report(
            progress,
            RegionSegmentationStage.BuildingAdjacency,
            0d,
            DescriptorEnd,
            completedIterations,
            settings.MaximumIterations);

        RegionAdjacencyGraph adjacency = RegionAdjacencyGraphBuilder.Build(
            request.Source,
            labels,
            regions,
            cancellationToken);
        Report(
            progress,
            RegionSegmentationStage.BuildingAdjacency,
            1d,
            AdjacencyEnd,
            completedIterations,
            settings.MaximumIterations);
        Report(
            progress,
            RegionSegmentationStage.BuildingHierarchy,
            0d,
            AdjacencyEnd,
            completedIterations,
            settings.MaximumIterations);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(
            regions,
            adjacency,
            request.MergeSettings,
            cancellationToken);
        int[] regionPixelCounts = regions.Select(region => region.PixelCount).ToArray();
        SegmentationDiagnostics diagnostics = new(
            completedIterations,
            converged,
            maximumDisplacement,
            connectivity.RawComponentCount,
            labels.RegionCount,
            connectivity.DisconnectedComponentsRepaired,
            connectivity.UndersizedComponentsMerged,
            RegionSizeDistribution.Create(regionPixelCounts));

        RegionSegmentationResult result = new(
            labels,
            regions,
            adjacency,
            hierarchy,
            diagnostics);

        Report(
            progress,
            RegionSegmentationStage.BuildingHierarchy,
            1d,
            HierarchyEnd,
            completedIterations,
            settings.MaximumIterations);
        Report(
            progress,
            RegionSegmentationStage.Completed,
            1d,
            1d,
            completedIterations,
            settings.MaximumIterations);
        return result;
    }

    private static RegionSegmentationResult CreateSingleRegionResult(
        RegionSegmentationRequest request,
        IProgress<RegionSegmentationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ImageSize size = request.Source.Size;
        int[] rawLabels = new int[checked((int)size.PixelCount)];
        RegionLabelMap labels = RegionLabelMap.Create(size, 1, rawLabels);
        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(
            request.Source,
            labels,
            cancellationToken);
        RegionAdjacencyGraph adjacency = RegionAdjacencyGraph.CreateEmpty(1);
        RegionHierarchy hierarchy = RegionHierarchy.CreateIdentity(1);
        SegmentationDiagnostics diagnostics = new(
            0,
            true,
            0d,
            1,
            1,
            regionSizes: RegionSizeDistribution.Create([checked((int)size.PixelCount)]));
        RegionSegmentationResult result = new(
            labels,
            regions,
            adjacency,
            hierarchy,
            diagnostics);
        Report(
            progress,
            RegionSegmentationStage.Completed,
            1d,
            1d,
            0,
            request.Settings.MaximumIterations);
        return result;
    }

    private static SlicColor[] ReadSource(
        IRgbaPixelSource source,
        RegionSegmentationSettings settings,
        IProgress<RegionSegmentationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ImageSize size = source.Size;
        SlicColor[] colors = new SlicColor[checked((int)size.PixelCount)];
        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double rowFraction = y / (double)size.Height;
                Report(
                    progress,
                    RegionSegmentationStage.Preparing,
                    rowFraction,
                    PreparingEnd * rowFraction,
                    0,
                    settings.MaximumIterations);
            }

            double normalizedY = (y + 0.5d) / size.Height;
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                double normalizedX = (x + 0.5d) / size.Width;
                Rgba32 pixel = source.SampleNearest(new NormalizedPoint(normalizedX, normalizedY));
                colors[rowOffset + x] = SlicColor.FromRgba(pixel);
            }
        }

        Report(
            progress,
            RegionSegmentationStage.Preparing,
            1d,
            PreparingEnd,
            0,
            settings.MaximumIterations);

        if (settings.PreBlurSigma > 0d)
        {
            ApplyGaussianBlur(
                colors,
                size,
                settings.PreBlurSigma,
                settings.MaximumIterations,
                progress,
                cancellationToken);
        }

        return colors;
    }

    private static void ApplyGaussianBlur(
        SlicColor[] colors,
        ImageSize size,
        double sigma,
        int maximumIterations,
        IProgress<RegionSegmentationProgress>? progress,
        CancellationToken cancellationToken)
    {
        double[] kernel = CreateGaussianKernel(sigma);
        int radius = kernel.Length / 2;
        int temporaryLength = Math.Max(size.Width, size.Height);
        float[] first = new float[temporaryLength];
        float[] second = new float[temporaryLength];
        float[] third = new float[temporaryLength];

        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double fraction = 0.5d * y / size.Height;
                Report(
                    progress,
                    RegionSegmentationStage.Smoothing,
                    fraction,
                    Interpolate(PreparingEnd, SmoothingEnd, fraction),
                    0,
                    maximumIterations);
            }

            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                SlicColor color = colors[rowOffset + x];
                first[x] = color.First;
                second[x] = color.Second;
                third[x] = color.Third;
            }

            for (int x = 0; x < size.Width; x++)
            {
                colors[rowOffset + x] = Convolve(
                    first,
                    second,
                    third,
                    size.Width,
                    x,
                    kernel,
                    radius);
            }
        }

        for (int x = 0; x < size.Width; x++)
        {
            if (x % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double fraction = 0.5d + (0.5d * x / size.Width);
                Report(
                    progress,
                    RegionSegmentationStage.Smoothing,
                    fraction,
                    Interpolate(PreparingEnd, SmoothingEnd, fraction),
                    0,
                    maximumIterations);
            }

            for (int y = 0; y < size.Height; y++)
            {
                SlicColor color = colors[checked((y * size.Width) + x)];
                first[y] = color.First;
                second[y] = color.Second;
                third[y] = color.Third;
            }

            for (int y = 0; y < size.Height; y++)
            {
                colors[checked((y * size.Width) + x)] = Convolve(
                    first,
                    second,
                    third,
                    size.Height,
                    y,
                    kernel,
                    radius);
            }
        }

        Report(
            progress,
            RegionSegmentationStage.Smoothing,
            1d,
            SmoothingEnd,
            0,
            maximumIterations);
    }

    private static SlicColor Convolve(
        float[] first,
        float[] second,
        float[] third,
        int length,
        int position,
        double[] kernel,
        int radius)
    {
        double firstSum = 0d;
        double secondSum = 0d;
        double thirdSum = 0d;
        for (int offset = -radius; offset <= radius; offset++)
        {
            int sourceIndex = Math.Clamp(position + offset, 0, length - 1);
            double weight = kernel[offset + radius];
            firstSum += first[sourceIndex] * weight;
            secondSum += second[sourceIndex] * weight;
            thirdSum += third[sourceIndex] * weight;
        }

        return new SlicColor((float)firstSum, (float)secondSum, (float)thirdSum);
    }

    private static double[] CreateGaussianKernel(double sigma)
    {
        int radius = Math.Max(1, checked((int)Math.Ceiling(3d * sigma)));
        double[] kernel = new double[checked((2 * radius) + 1)];
        double denominator = 2d * sigma * sigma;
        double sum = 0d;
        for (int offset = -radius; offset <= radius; offset++)
        {
            double weight = Math.Exp(-(offset * offset) / denominator);
            kernel[offset + radius] = weight;
            sum += weight;
        }

        for (int index = 0; index < kernel.Length; index++)
        {
            kernel[index] /= sum;
        }

        return kernel;
    }

    private static void ConvertToLab(
        SlicColor[] colors,
        ImageSize size,
        int maximumIterations,
        IProgress<RegionSegmentationProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double fraction = y / (double)size.Height;
                Report(
                    progress,
                    RegionSegmentationStage.ConvertingColor,
                    fraction,
                    Interpolate(SmoothingEnd, ColorConversionEnd, fraction),
                    0,
                    maximumIterations);
            }

            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                colors[rowOffset + x] = colors[rowOffset + x].ToLab();
            }
        }

        Report(
            progress,
            RegionSegmentationStage.ConvertingColor,
            1d,
            ColorConversionEnd,
            0,
            maximumIterations);
    }

    private static SlicCluster[] InitializeClusters(
        SlicColor[] colors,
        ImageSize size,
        GridGeometry grid,
        int maximumIterations,
        IProgress<RegionSegmentationProgress>? progress,
        CancellationToken cancellationToken)
    {
        SlicCluster[] clusters = new SlicCluster[checked(grid.Columns * grid.Rows)];
        int clusterId = 0;
        for (int row = 0; row < grid.Rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int initialY = Math.Clamp(
                checked((int)Math.Floor((row + 0.5d) * grid.SpacingY)),
                0,
                size.Height - 1);

            for (int column = 0; column < grid.Columns; column++)
            {
                int initialX = Math.Clamp(
                    checked((int)Math.Floor((column + 0.5d) * grid.SpacingX)),
                    0,
                    size.Width - 1);
                (int X, int Y) adjusted = FindLowestGradientPixel(
                    colors,
                    size,
                    initialX,
                    initialY);
                SlicColor color = colors[checked((adjusted.Y * size.Width) + adjusted.X)];
                clusters[clusterId++] = new SlicCluster(
                    color.First,
                    color.Second,
                    color.Third,
                    adjusted.X,
                    adjusted.Y);
            }

            double fraction = (row + 1d) / grid.Rows;
            Report(
                progress,
                RegionSegmentationStage.InitializingClusters,
                fraction,
                Interpolate(ColorConversionEnd, InitializationEnd, fraction),
                0,
                maximumIterations);
        }

        return clusters;
    }

    private static (int X, int Y) FindLowestGradientPixel(
        SlicColor[] colors,
        ImageSize size,
        int centerX,
        int centerY)
    {
        int bestX = centerX;
        int bestY = centerY;
        double bestGradient = double.PositiveInfinity;

        for (int y = Math.Max(0, centerY - 1); y <= Math.Min(size.Height - 1, centerY + 1); y++)
        {
            for (int x = Math.Max(0, centerX - 1); x <= Math.Min(size.Width - 1, centerX + 1); x++)
            {
                double gradient = CalculateGradient(colors, size, x, y);
                if (gradient < bestGradient)
                {
                    bestGradient = gradient;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        return (bestX, bestY);
    }

    private static double CalculateGradient(
        SlicColor[] colors,
        ImageSize size,
        int x,
        int y)
    {
        SlicColor left = GetColor(colors, size, x - 1, y);
        SlicColor right = GetColor(colors, size, x + 1, y);
        SlicColor top = GetColor(colors, size, x, y - 1);
        SlicColor bottom = GetColor(colors, size, x, y + 1);
        return ColorDistanceSquared(left, right) + ColorDistanceSquared(top, bottom);
    }

    private static void AssignPixels(
        SlicColor[] colors,
        ImageSize size,
        SlicCluster[] clusters,
        GridGeometry grid,
        double compactness,
        int[] assignments,
        float[] distances,
        CancellationToken cancellationToken)
    {
        Array.Fill(assignments, -1);
        Array.Fill(distances, float.PositiveInfinity);
        double spatialWeight = compactness * compactness;

        for (int clusterId = 0; clusterId < clusters.Length; clusterId++)
        {
            if (clusterId % 64 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            SlicCluster cluster = clusters[clusterId];
            int left = Math.Max(0, checked((int)Math.Floor(cluster.X - grid.SpacingX)));
            int right = Math.Min(size.Width - 1, checked((int)Math.Ceiling(cluster.X + grid.SpacingX)));
            int top = Math.Max(0, checked((int)Math.Floor(cluster.Y - grid.SpacingY)));
            int bottom = Math.Min(size.Height - 1, checked((int)Math.Ceiling(cluster.Y + grid.SpacingY)));

            for (int y = top; y <= bottom; y++)
            {
                int rowOffset = checked(y * size.Width);
                for (int x = left; x <= right; x++)
                {
                    int pixelIndex = rowOffset + x;
                    SlicColor color = colors[pixelIndex];
                    double colorDistance = ColorDistanceSquared(color, cluster);
                    double normalizedX = (x - cluster.X) / grid.SpacingX;
                    double normalizedY = (y - cluster.Y) / grid.SpacingY;
                    double distance = colorDistance
                        + (spatialWeight * ((normalizedX * normalizedX) + (normalizedY * normalizedY)));

                    if (distance < distances[pixelIndex])
                    {
                        distances[pixelIndex] = (float)distance;
                        assignments[pixelIndex] = clusterId;
                    }
                }
            }
        }

        for (int index = 0; index < assignments.Length; index++)
        {
            if (assignments[index] >= 0)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            int x = index % size.Width;
            int y = index / size.Width;
            SlicColor color = colors[index];
            int bestClusterId = 0;
            double bestDistance = double.PositiveInfinity;
            for (int clusterId = 0; clusterId < clusters.Length; clusterId++)
            {
                SlicCluster cluster = clusters[clusterId];
                double colorDistance = ColorDistanceSquared(color, cluster);
                double normalizedX = (x - cluster.X) / grid.SpacingX;
                double normalizedY = (y - cluster.Y) / grid.SpacingY;
                double distance = colorDistance
                    + (spatialWeight * ((normalizedX * normalizedX) + (normalizedY * normalizedY)));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestClusterId = clusterId;
                }
            }

            distances[index] = (float)bestDistance;
            assignments[index] = bestClusterId;
        }
    }

    private static double UpdateClusters(
        SlicColor[] colors,
        ImageSize size,
        SlicCluster[] clusters,
        int[] assignments,
        ClusterAccumulator[] accumulators,
        CancellationToken cancellationToken)
    {
        Array.Clear(accumulators);
        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                int index = rowOffset + x;
                int clusterId = assignments[index];
                SlicColor color = colors[index];
                accumulators[clusterId].Add(color, x, y);
            }
        }

        double maximumDisplacement = 0d;
        for (int clusterId = 0; clusterId < clusters.Length; clusterId++)
        {
            ClusterAccumulator accumulator = accumulators[clusterId];
            if (accumulator.Count == 0)
            {
                continue;
            }

            SlicCluster previous = clusters[clusterId];
            SlicCluster updated = accumulator.CreateCluster();
            double deltaX = updated.X - previous.X;
            double deltaY = updated.Y - previous.Y;
            maximumDisplacement = Math.Max(
                maximumDisplacement,
                Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)));
            clusters[clusterId] = updated;
        }

        return maximumDisplacement;
    }

    private static int CalculateMinimumRegionPixelCount(ImageSize size, int clusterCount)
    {
        double expectedRegionArea = size.PixelCount / (double)clusterCount;
        return Math.Max(1, checked((int)Math.Floor(expectedRegionArea * MinimumComponentAreaFraction)));
    }

    private static SlicColor GetColor(
        SlicColor[] colors,
        ImageSize size,
        int x,
        int y)
    {
        int clampedX = Math.Clamp(x, 0, size.Width - 1);
        int clampedY = Math.Clamp(y, 0, size.Height - 1);
        return colors[checked((clampedY * size.Width) + clampedX)];
    }

    private static double ColorDistanceSquared(SlicColor first, SlicColor second)
    {
        double firstDifference = first.First - second.First;
        double secondDifference = first.Second - second.Second;
        double thirdDifference = first.Third - second.Third;
        return (firstDifference * firstDifference)
            + (secondDifference * secondDifference)
            + (thirdDifference * thirdDifference);
    }

    private static double ColorDistanceSquared(SlicColor color, SlicCluster cluster)
    {
        double firstDifference = color.First - cluster.First;
        double secondDifference = color.Second - cluster.Second;
        double thirdDifference = color.Third - cluster.Third;
        return (firstDifference * firstDifference)
            + (secondDifference * secondDifference)
            + (thirdDifference * thirdDifference);
    }

    private static double Interpolate(double start, double end, double fraction)
    {
        return start + ((end - start) * Math.Clamp(fraction, 0d, 1d));
    }

    private static void Report(
        IProgress<RegionSegmentationProgress>? progress,
        RegionSegmentationStage stage,
        double stageFraction,
        double overallFraction,
        int completedIterations,
        int totalIterations)
    {
        progress?.Report(new RegionSegmentationProgress(
            stage,
            stageFraction,
            overallFraction,
            completedIterations,
            totalIterations));
    }

    private readonly record struct GridGeometry(
        int Columns,
        int Rows,
        double SpacingX,
        double SpacingY)
    {
        public static GridGeometry Create(ImageSize size, int targetRegionSize)
        {
            int columns = DivideRoundUp(size.Width, targetRegionSize);
            int rows = DivideRoundUp(size.Height, targetRegionSize);
            return new GridGeometry(
                columns,
                rows,
                size.Width / (double)columns,
                size.Height / (double)rows);
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            return checked((value + divisor - 1) / divisor);
        }
    }

    private record struct SlicColor
    {
        public SlicColor(float first, float second, float third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public float First { get; set; }

        public float Second { get; set; }

        public float Third { get; set; }

        public static SlicColor FromRgba(Rgba32 color)
        {
            double alpha = color.Alpha / (double)byte.MaxValue;
            float red = (float)(((color.Red / (double)byte.MaxValue) * alpha) + (1d - alpha));
            float green = (float)(((color.Green / (double)byte.MaxValue) * alpha) + (1d - alpha));
            float blue = (float)(((color.Blue / (double)byte.MaxValue) * alpha) + (1d - alpha));
            return new SlicColor(red, green, blue);
        }

        public SlicColor ToLab()
        {
            double red = ToLinearRgb(First);
            double green = ToLinearRgb(Second);
            double blue = ToLinearRgb(Third);

            double x = ((0.4124564d * red) + (0.3575761d * green) + (0.1804375d * blue)) / 0.95047d;
            double y = (0.2126729d * red) + (0.7151522d * green) + (0.072175d * blue);
            double z = ((0.0193339d * red) + (0.119192d * green) + (0.9503041d * blue)) / 1.08883d;

            double transformedX = TransformLabCoordinate(x);
            double transformedY = TransformLabCoordinate(y);
            double transformedZ = TransformLabCoordinate(z);
            return new SlicColor(
                (float)((116d * transformedY) - 16d),
                (float)(500d * (transformedX - transformedY)),
                (float)(200d * (transformedY - transformedZ)));
        }

        private static double ToLinearRgb(double value)
        {
            return value <= 0.04045d
                ? value / 12.92d
                : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
        }

        private static double TransformLabCoordinate(double value)
        {
            const double delta = 6d / 29d;
            double threshold = delta * delta * delta;
            return value > threshold
                ? Math.Cbrt(value)
                : (value / (3d * delta * delta)) + (4d / 29d);
        }
    }

    private readonly record struct SlicCluster(
        double First,
        double Second,
        double Third,
        double X,
        double Y);

    private record struct ClusterAccumulator
    {
        public double FirstSum { get; private set; }

        public double SecondSum { get; private set; }

        public double ThirdSum { get; private set; }

        public double XSum { get; private set; }

        public double YSum { get; private set; }

        public int Count { get; private set; }

        public void Add(SlicColor color, int x, int y)
        {
            FirstSum += color.First;
            SecondSum += color.Second;
            ThirdSum += color.Third;
            XSum += x;
            YSum += y;
            Count++;
        }

        public SlicCluster CreateCluster()
        {
            if (Count <= 0)
            {
                throw new InvalidOperationException("An empty accumulator cannot create a SLIC cluster.");
            }

            return new SlicCluster(
                FirstSum / Count,
                SecondSum / Count,
                ThirdSum / Count,
                XSum / Count,
                YSum / Count);
        }
    }
}
