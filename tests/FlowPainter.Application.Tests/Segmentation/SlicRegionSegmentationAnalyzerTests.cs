using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class SlicRegionSegmentationAnalyzerTests
{
    private static readonly int[] ExpectedUniformRegionCounts = [16, 16, 16, 16];

    [Fact]
    public async Task AnalyzeAsyncProducesExpectedGridForUniformImage()
    {
        RgbaImage image = CreateUniformImage(8, 8, Rgba32.Opaque(120, 120, 120));

        RegionSegmentationResult result = await AnalyzeAsync(
            image,
            targetRegionSize: 4,
            preBlurSigma: 0d);

        Assert.Equal(4, result.Labels.RegionCount);
        Assert.Equal(ExpectedUniformRegionCounts, result.Labels.CountPixelsByRegion());
    }

    [Fact]
    public async Task AnalyzeAsyncIsDeterministic()
    {
        RgbaImage image = CreateQuadrantImage(12, 12);
        RegionSegmentationSettings settings = new(
            targetRegionSize: 4,
            compactness: 8d,
            preBlurSigma: 0.6d,
            maximumIterations: 12,
            convergenceTolerance: 0.05d);
        SlicRegionSegmentationAnalyzer analyzer = new();
        RegionSegmentationRequest request = new(image, settings);

        RegionSegmentationResult first = await analyzer.AnalyzeAsync(request);
        RegionSegmentationResult second = await analyzer.AnalyzeAsync(request);

        Assert.Equal(first.Labels.CopyLabels(), second.Labels.CopyLabels());
        Assert.Equal(first.Diagnostics.IterationCount, second.Diagnostics.IterationCount);
        Assert.Equal(first.Diagnostics.FinalMaximumDisplacement, second.Diagnostics.FinalMaximumDisplacement);
    }

    [Fact]
    public async Task AnalyzeAsyncSeparatesStrongVerticalColorBoundary()
    {
        RgbaImage image = CreateVerticalSplitImage(8, 4);

        RegionSegmentationResult result = await AnalyzeAsync(
            image,
            targetRegionSize: 4,
            compactness: 5d,
            preBlurSigma: 0d);

        uint left = result.Labels[1, 1];
        uint right = result.Labels[6, 1];
        Assert.NotEqual(left, right);
        Assert.All(Enumerable.Range(0, 4), x => Assert.Equal(left, result.Labels[x, 1]));
        Assert.All(Enumerable.Range(4, 4), x => Assert.Equal(right, result.Labels[x, 1]));
    }

    [Fact]
    public async Task AnalyzeAsyncReturnsSingleRegionForOnePixel()
    {
        RgbaImage image = CreateUniformImage(1, 1, Rgba32.Opaque(20, 40, 60));

        RegionSegmentationResult result = await AnalyzeAsync(
            image,
            targetRegionSize: 4,
            preBlurSigma: 0d);

        Assert.Equal(1, result.Labels.RegionCount);
        Assert.Equal(0u, result.Labels[0, 0]);
        Assert.Equal(1, result.Regions[0].PixelCount);
        Assert.Equal(new PixelBounds(0, 0, 1, 1), result.Regions[0].Bounds);
        Assert.Equal(new RegionCentroid(0.5d, 0.5d), result.Regions[0].Centroid);
        Assert.NotSame(RegionVisualDescriptors.Empty, result.Regions[0].Descriptors);
        Assert.Equal(4d, result.Regions[0].Descriptors.Perimeter);
        Assert.True(result.Regions[0].Descriptors.MeanLightness > 0d);
    }

    [Fact]
    public async Task AnalyzeAsyncDisabledSegmentationReturnsOneCompleteRegion()
    {
        RgbaImage image = CreateQuadrantImage(8, 6);
        SlicRegionSegmentationAnalyzer analyzer = new();
        RegionSegmentationRequest request = new(
            image,
            new RegionSegmentationSettings(enabled: false));

        RegionSegmentationResult result = await analyzer.AnalyzeAsync(request);

        Assert.Equal(1, result.Labels.RegionCount);
        Assert.All(result.Labels.CopyLabels(), label => Assert.Equal(0u, label));
        Assert.Equal(image.Size.PixelCount, result.Regions[0].PixelCount);
        Assert.Empty(result.Adjacency.Edges);
        Assert.Single(result.Hierarchy.Levels);
        Assert.True(result.Diagnostics.Converged);
    }

    [Fact]
    public async Task AnalyzeAsyncBuildsRegionBasicsMatchingLabels()
    {
        RgbaImage image = CreateQuadrantImage(10, 6);

        RegionSegmentationResult result = await AnalyzeAsync(
            image,
            targetRegionSize: 4,
            preBlurSigma: 0d);
        int[] counts = result.Labels.CountPixelsByRegion();

        Assert.Equal(result.Labels.RegionCount, result.Regions.Count);
        Assert.Equal(image.Size.PixelCount, counts.Sum(count => (long)count));
        foreach (ImageRegion region in result.Regions)
        {
            Assert.Equal(counts[region.Id], region.PixelCount);
            Assert.True(region.Bounds.Contains(
                (int)Math.Floor(region.Centroid.X),
                (int)Math.Floor(region.Centroid.Y)));
        }
    }

    [Fact]
    public async Task AnalyzeAsyncReturnsThreeScaleHierarchyAndCompleteAdjacency()
    {
        RegionSegmentationResult result = await AnalyzeAsync(
            CreateQuadrantImage(8, 8),
            targetRegionSize: 4,
            preBlurSigma: 0d);

        Assert.NotEmpty(result.Adjacency.Edges);
        Assert.All(result.Adjacency.Edges, edge => Assert.True(edge.SharedBoundaryLength > 0));
        Assert.Equal(3, result.Hierarchy.Levels.Count);
        Assert.Equal(result.Labels.RegionCount, result.Hierarchy.FineRegionCount);
        Assert.True(
            result.Hierarchy.Levels[1].ParentRegionCount
            <= result.Hierarchy.Levels[0].ParentRegionCount);
        Assert.True(
            result.Hierarchy.Levels[2].ParentRegionCount
            <= result.Hierarchy.Levels[1].ParentRegionCount);
        for (int regionId = 0; regionId < result.Labels.RegionCount; regionId++)
        {
            Assert.Equal(regionId, result.Hierarchy.Levels[0].GetParentId(regionId));
        }
    }

    [Fact]
    public async Task AnalyzeAsyncReportsMonotonicProgress()
    {
        RecordingProgress progress = new();
        RegionSegmentationSettings settings = new(
            targetRegionSize: 4,
            compactness: 10d,
            preBlurSigma: 0.8d,
            maximumIterations: 6,
            convergenceTolerance: 0.01d);

        _ = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(CreateQuadrantImage(32, 24), settings),
            progress);

        Assert.NotEmpty(progress.Values);
        Assert.Equal(RegionSegmentationStage.Preparing, progress.Values[0].Stage);
        Assert.Equal(RegionSegmentationStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].OverallFraction);
        for (int index = 1; index < progress.Values.Count; index++)
        {
            Assert.True(
                progress.Values[index].OverallFraction >= progress.Values[index - 1].OverallFraction,
                $"Progress moved backwards at item {index}.");
        }
    }

    [Fact]
    public async Task AnalyzeAsyncReportsSmoothingWhenEnabled()
    {
        RecordingProgress progress = new();

        _ = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(
                CreateUniformImage(16, 16, Rgba32.Opaque(100, 120, 140)),
                new RegionSegmentationSettings(targetRegionSize: 4, preBlurSigma: 1d)),
            progress);

        Assert.Contains(progress.Values, value => value.Stage == RegionSegmentationStage.Smoothing);
    }

    [Fact]
    public async Task AnalyzeAsyncSkipsSmoothingWhenDisabled()
    {
        RecordingProgress progress = new();

        _ = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(
                CreateUniformImage(16, 16, Rgba32.Opaque(100, 120, 140)),
                new RegionSegmentationSettings(targetRegionSize: 4, preBlurSigma: 0d)),
            progress);

        Assert.DoesNotContain(progress.Values, value => value.Stage == RegionSegmentationStage.Smoothing);
    }

    [Fact]
    public async Task AnalyzeAsyncHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
                new RegionSegmentationRequest(
                    CreateUniformImage(8, 8, Rgba32.Opaque(0, 0, 0)),
                    new RegionSegmentationSettings(targetRegionSize: 4)),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task AnalyzeAsyncCanBeCancelledDuringSampling()
    {
        using CancellationTokenSource cancellation = new();
        CancellingPixelSource source = new(new ImageSize(32, 32), cancellation, 20);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
                new RegionSegmentationRequest(
                    source,
                    new RegionSegmentationSettings(targetRegionSize: 4, preBlurSigma: 0d)),
                cancellationToken: cancellation.Token));

        Assert.True(source.SampleCount >= 20);
    }

    [Fact]
    public async Task AnalyzeAsyncDoesNotMutateSource()
    {
        RgbaImage image = CreateQuadrantImage(12, 8);
        Rgba32[] before = image.CopyPixels();

        _ = await AnalyzeAsync(image, targetRegionSize: 4, preBlurSigma: 1.2d);

        Assert.Equal(before, image.CopyPixels());
    }

    [Fact]
    public async Task AnalyzeAsyncConvergesWithGenerousTolerance()
    {
        RegionSegmentationSettings settings = new(
            targetRegionSize: 4,
            preBlurSigma: 0d,
            maximumIterations: 10,
            convergenceTolerance: 100d);

        RegionSegmentationResult result = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(CreateQuadrantImage(16, 16), settings));

        Assert.True(result.Diagnostics.Converged);
        Assert.Equal(1, result.Diagnostics.IterationCount);
        Assert.InRange(result.Diagnostics.FinalMaximumDisplacement, 0d, 100d);
    }

    [Fact]
    public async Task AnalyzeAsyncRespectsMaximumIterationBound()
    {
        RegionSegmentationSettings settings = new(
            targetRegionSize: 4,
            preBlurSigma: 0d,
            maximumIterations: 1,
            convergenceTolerance: 0.000001d);

        RegionSegmentationResult result = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(CreateQuadrantImage(16, 16), settings));

        Assert.Equal(1, result.Diagnostics.IterationCount);
        Assert.True(double.IsFinite(result.Diagnostics.FinalMaximumDisplacement));
        Assert.True(result.Diagnostics.FinalMaximumDisplacement >= 0d);
    }

    [Fact]
    public async Task AnalyzeAsyncUsesCompactStorageForNormalGrid()
    {
        RegionSegmentationResult result = await AnalyzeAsync(
            CreateQuadrantImage(32, 32),
            targetRegionSize: 4,
            preBlurSigma: 0d);

        Assert.Equal(RegionLabelStorageKind.Compact, result.Labels.StorageKind);
    }

    [Fact]
    public async Task AnalyzeAsyncCompositesTransparentPixelsAgainstWhite()
    {
        Rgba32[] transparentPixels = new Rgba32[64];
        Array.Fill(transparentPixels, new Rgba32(0, 0, 0, 0));
        RgbaImage transparent = new(new ImageSize(8, 8), transparentPixels);
        RgbaImage white = CreateUniformImage(8, 8, Rgba32.Opaque(255, 255, 255));

        RegionSegmentationResult transparentResult = await AnalyzeAsync(
            transparent,
            targetRegionSize: 4,
            preBlurSigma: 0d);
        RegionSegmentationResult whiteResult = await AnalyzeAsync(
            white,
            targetRegionSize: 4,
            preBlurSigma: 0d);

        Assert.Equal(whiteResult.Labels.CopyLabels(), transparentResult.Labels.CopyLabels());
    }

    [Fact]
    public async Task AnalyzeAsyncRejectsOverBudgetRequestBeforeSampling()
    {
        CountingPixelSource source = new(new ImageSize(10_000, 10_000));
        RegionSegmentationSettings settings = new(
            targetRegionSize: 4,
            compactness: 10d,
            preBlurSigma: 1d,
            maximumIterations: 100,
            convergenceTolerance: 0.5d);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
                new RegionSegmentationRequest(source, settings)));

        Assert.Contains("working-set budget", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, source.SampleCount);
    }

    [Fact]
    public async Task AnalyzeAsyncProducesConnectedFinalRegions()
    {
        RegionSegmentationResult result = await AnalyzeAsync(
            CreateQuadrantImage(24, 20),
            targetRegionSize: 4,
            preBlurSigma: 0d);

        AssertAllRegionsConnected(result.Labels);
    }

    [Fact]
    public async Task AnalyzeAsyncReportsConnectivityStage()
    {
        RecordingProgress progress = new();

        _ = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(
                CreateQuadrantImage(16, 16),
                new RegionSegmentationSettings(targetRegionSize: 4, preBlurSigma: 0d)),
            progress);

        Assert.Contains(
            progress.Values,
            value => value.Stage == RegionSegmentationStage.RepairingConnectivity);
        Assert.Contains(
            progress.Values,
            value => value.Stage == RegionSegmentationStage.BuildingAdjacency);
        Assert.Contains(
            progress.Values,
            value => value.Stage == RegionSegmentationStage.BuildingHierarchy);
    }

    [Fact]
    public async Task AnalyzeAsyncReportsRegionSizeDistribution()
    {
        RegionSegmentationResult result = await AnalyzeAsync(
            CreateQuadrantImage(16, 16),
            targetRegionSize: 4,
            preBlurSigma: 0d);

        RegionSizeDistribution distribution = Assert.IsType<RegionSizeDistribution>(
            result.Diagnostics.RegionSizes);
        int[] counts = result.Labels.CountPixelsByRegion();
        Assert.Equal(counts.Min(), distribution.MinimumPixelCount);
        Assert.Equal(counts.Max(), distribution.MaximumPixelCount);
        Assert.Equal(counts.Average(), distribution.MeanPixelCount, 12);
    }

    [Fact]
    public async Task AnalyzeAsyncReportsTopologyRepairCounts()
    {
        RegionSegmentationResult result = await AnalyzeAsync(
            CreateQuadrantImage(16, 16),
            targetRegionSize: 4,
            preBlurSigma: 0d);

        Assert.True(result.Diagnostics.DisconnectedComponentsRepaired >= 0);
        Assert.True(result.Diagnostics.UndersizedComponentsMerged >= 0);
        Assert.InRange(
            result.Diagnostics.FinalRegionCount,
            1,
            result.Diagnostics.RawRegionCount);
    }

    [Fact]
    public async Task AnalyzeAsyncUsesRequestMergeSettings()
    {
        RgbaImage image = CreateUniformImage(8, 8, Rgba32.Opaque(120, 120, 120));
        RegionSegmentationSettings segmentationSettings = new(
            targetRegionSize: 4,
            preBlurSigma: 0d);
        RegionMergeSettings mergeSettings = new(
            intermediateTargetRatio: 0.5d,
            broadMassTargetRatio: 0.25d,
            intermediateMaximumCost: 1d,
            broadMassMaximumCost: 1d,
            strongBoundaryThreshold: 1d,
            maximumParentAreaFraction: 1d);

        RegionSegmentationResult result = await new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(
                image,
                segmentationSettings,
                mergeSettings: mergeSettings));

        Assert.Equal(4, result.Hierarchy.Levels[0].ParentRegionCount);
        Assert.Equal(2, result.Hierarchy.Levels[1].ParentRegionCount);
        Assert.Equal(1, result.Hierarchy.Levels[2].ParentRegionCount);
    }

    private static void AssertAllRegionsConnected(RegionLabelMap labels)
    {
        bool[] visited = new bool[checked((int)labels.Size.PixelCount)];
        int[] componentCounts = new int[labels.RegionCount];
        Queue<int> queue = new();

        for (int start = 0; start < visited.Length; start++)
        {
            if (visited[start])
            {
                continue;
            }

            int startX = start % labels.Size.Width;
            int startY = start / labels.Size.Width;
            uint regionId = labels[startX, startY];
            componentCounts[checked((int)regionId)]++;
            visited[start] = true;
            queue.Enqueue(start);

            while (queue.TryDequeue(out int current))
            {
                int x = current % labels.Size.Width;
                int y = current / labels.Size.Width;
                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            void TryEnqueue(int x, int y)
            {
                if (x < 0
                    || x >= labels.Size.Width
                    || y < 0
                    || y >= labels.Size.Height
                    || labels[x, y] != regionId)
                {
                    return;
                }

                int index = checked((y * labels.Size.Width) + x);
                if (!visited[index])
                {
                    visited[index] = true;
                    queue.Enqueue(index);
                }
            }
        }

        Assert.All(componentCounts, count => Assert.Equal(1, count));
    }

    private static Task<RegionSegmentationResult> AnalyzeAsync(
        RgbaImage image,
        int targetRegionSize,
        double compactness = 10d,
        double preBlurSigma = 0d)
    {
        RegionSegmentationSettings settings = new(
            targetRegionSize,
            compactness,
            preBlurSigma,
            maximumIterations: 20,
            convergenceTolerance: 0.001d);
        return new SlicRegionSegmentationAnalyzer().AnalyzeAsync(
            new RegionSegmentationRequest(image, settings));
    }

    private static RgbaImage CreateUniformImage(
        int width,
        int height,
        Rgba32 color)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, color);
        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private static RgbaImage CreateVerticalSplitImage(int width, int height)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[checked((y * width) + x)] = x < width / 2
                    ? Rgba32.Opaque(20, 40, 180)
                    : Rgba32.Opaque(230, 210, 30);
            }
        }

        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private static RgbaImage CreateQuadrantImage(int width, int height)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool right = x >= width / 2;
                bool bottom = y >= height / 2;
                pixels[checked((y * width) + x)] = (right, bottom) switch
                {
                    (false, false) => Rgba32.Opaque(220, 40, 40),
                    (true, false) => Rgba32.Opaque(40, 200, 70),
                    (false, true) => Rgba32.Opaque(40, 80, 220),
                    _ => Rgba32.Opaque(230, 210, 50),
                };
            }
        }

        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private sealed class RecordingProgress : IProgress<RegionSegmentationProgress>
    {
        public List<RegionSegmentationProgress> Values { get; } = [];

        public void Report(RegionSegmentationProgress value)
        {
            Values.Add(value);
        }
    }

    private sealed class CancellingPixelSource : IRgbaPixelSource
    {
        private readonly CancellationTokenSource _cancellation;
        private readonly int _cancelAfterSamples;

        public CancellingPixelSource(
            ImageSize size,
            CancellationTokenSource cancellation,
            int cancelAfterSamples)
        {
            Size = size;
            _cancellation = cancellation;
            _cancelAfterSamples = cancelAfterSamples;
        }

        public ImageSize Size { get; }

        public int SampleCount { get; private set; }

        public Rgba32 SampleNearest(NormalizedPoint point)
        {
            _ = point;
            SampleCount++;
            if (SampleCount == _cancelAfterSamples)
            {
                _cancellation.Cancel();
            }

            return Rgba32.Opaque(100, 120, 140);
        }
    }

    private sealed class CountingPixelSource : IRgbaPixelSource
    {
        public CountingPixelSource(ImageSize size)
        {
            Size = size;
        }

        public ImageSize Size { get; }

        public int SampleCount { get; private set; }

        public Rgba32 SampleNearest(NormalizedPoint point)
        {
            _ = point;
            SampleCount++;
            return Rgba32.Opaque(0, 0, 0);
        }
    }
}
