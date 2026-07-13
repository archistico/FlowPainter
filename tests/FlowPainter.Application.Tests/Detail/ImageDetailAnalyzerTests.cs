using FlowPainter.Application.Detail;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Detail;

public sealed class ImageDetailAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsyncReturnsBaseDetailForUniformImage()
    {
        RgbaImage image = CreateUniformImage(5, 4, Rgba32.Opaque(120, 120, 120));
        DetailAnalysisSettings settings = new(
            baseDetail: 0.2d,
            edgeWeight: 1d,
            contrastWeight: 1d,
            smoothingRadius: 0);

        DetailMap map = await new ImageDetailAnalyzer().AnalyzeAsync(image, settings);

        Assert.Equal(image.Size, map.Size);
        Assert.All(map.CopyValues(), value => Assert.InRange(value, 0.199999f, 0.200001f));
    }

    [Fact]
    public async Task AnalyzeAsyncAssignsMoreDetailToHardEdge()
    {
        RgbaImage image = CreateVerticalSplitImage(8, 4);
        DetailAnalysisSettings settings = new(
            baseDetail: 0.1d,
            edgeWeight: 1d,
            contrastWeight: 0.5d,
            smoothingRadius: 0);

        DetailMap map = await new ImageDetailAnalyzer().AnalyzeAsync(image, settings);

        Assert.True(map[3, 2] > map[0, 2]);
        Assert.True(map[4, 2] > map[7, 2]);
    }

    [Fact]
    public async Task AnalyzeAsyncDetectsChromaticContrast()
    {
        RgbaImage image = new(
            new ImageSize(3, 1),
            [
                Rgba32.Opaque(255, 0, 0),
                Rgba32.Opaque(0, 255, 255),
                Rgba32.Opaque(255, 0, 0)
            ]);
        DetailAnalysisSettings settings = new(
            baseDetail: 0d,
            edgeWeight: 0.01d,
            contrastWeight: 1d,
            smoothingRadius: 0);

        DetailMap map = await new ImageDetailAnalyzer().AnalyzeAsync(image, settings);

        Assert.True(map[1, 0] > map[0, 0]);
    }

    [Fact]
    public async Task AnalyzeAsyncSmoothingSpreadsEdgeImportance()
    {
        RgbaImage image = CreateVerticalSplitImage(9, 3);
        ImageDetailAnalyzer analyzer = new();
        DetailMap sharp = await analyzer.AnalyzeAsync(
            image,
            new DetailAnalysisSettings(smoothingRadius: 0));
        DetailMap smooth = await analyzer.AnalyzeAsync(
            image,
            new DetailAnalysisSettings(smoothingRadius: 2));

        Assert.True(smooth[2, 1] > sharp[2, 1]);
        Assert.True(smooth[4, 1] < sharp[4, 1]);
    }

    [Fact]
    public async Task AnalyzeAsyncIsDeterministic()
    {
        RgbaImage image = CreateVerticalSplitImage(8, 8);
        ImageDetailAnalyzer analyzer = new();
        DetailAnalysisSettings settings = new();

        DetailMap first = await analyzer.AnalyzeAsync(image, settings);
        DetailMap second = await analyzer.AnalyzeAsync(image, settings);

        Assert.Equal(first.CopyValues(), second.CopyValues());
    }

    [Fact]
    public async Task AnalyzeAsyncReportsOrderedStages()
    {
        RgbaImage image = CreateVerticalSplitImage(32, 32);
        RecordingProgress progress = new();

        _ = await new ImageDetailAnalyzer().AnalyzeAsync(
            image,
            new DetailAnalysisSettings(),
            progress);

        Assert.Equal(DetailAnalysisStage.Preparing, progress.Values[0].Stage);
        Assert.Contains(progress.Values, value => value.Stage == DetailAnalysisStage.AnalyzingStructure);
        Assert.Contains(progress.Values, value => value.Stage == DetailAnalysisStage.Smoothing);
        Assert.Equal(DetailAnalysisStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public async Task AnalyzeAsyncHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ImageDetailAnalyzer().AnalyzeAsync(
                CreateUniformImage(2, 2, Rgba32.Opaque(0, 0, 0)),
                new DetailAnalysisSettings(),
                cancellationToken: cancellation.Token));
    }

    private static RgbaImage CreateVerticalSplitImage(int width, int height)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[checked((y * width) + x)] = x < width / 2
                    ? Rgba32.Opaque(0, 0, 0)
                    : Rgba32.Opaque(255, 255, 255);
            }
        }

        return new RgbaImage(new ImageSize(width, height), pixels);
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

    private sealed class RecordingProgress : IProgress<DetailAnalysisProgress>
    {
        public List<DetailAnalysisProgress> Values { get; } = [];

        public void Report(DetailAnalysisProgress value)
        {
            Values.Add(value);
        }
    }
}
