using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Semantics;

public sealed class HeuristicSemanticImportanceAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsyncReturnsEmptySemanticMapsWhenDisabled()
    {
        RgbaImage image = CreateSubjectImage(32, 24);

        SemanticAnalysisResult result = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
            image,
            new SemanticAnalysisSettings(enabled: false));

        Assert.Empty(result.Regions);
        Assert.All(result.ImportanceMap.CopyValues(), value => Assert.Equal(0f, value));
    }

    [Fact]
    public async Task AnalyzeAsyncReturnsNoSubjectForUniformImage()
    {
        RgbaImage image = CreateUniformImage(32, 24, Rgba32.Opaque(100, 100, 100));

        SemanticAnalysisResult result = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
            image,
            CreateSensitiveSettings());

        Assert.Empty(result.Regions);
        Assert.All(result.ImportanceMap.CopyValues(), value => Assert.Equal(0f, value));
    }

    [Fact]
    public async Task AnalyzeAsyncEmphasizesContrastingCentralSubject()
    {
        RgbaImage image = CreateSubjectImage(48, 36);

        SemanticAnalysisResult result = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
            image,
            CreateSensitiveSettings());

        float center = result.ImportanceMap[24, 18];
        float background = result.ImportanceMap[2, 2];
        Assert.True(center > background);
        Assert.Contains(result.Regions, region => region.Role == SemanticRegionRole.Subject);
        Assert.Contains(
            result.Regions,
            region => region.Role is SemanticRegionRole.FocalArea or SemanticRegionRole.CriticalDetail);
    }

    [Fact]
    public async Task AnalyzeAsyncBuildsSeparateSubjectSilhouetteAndFocalMaps()
    {
        RgbaImage image = CreateSubjectImage(48, 36);

        SemanticAnalysisResult result = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
            image,
            CreateSensitiveSettings());

        Assert.Contains(result.SubjectMap.CopyValues(), value => value > 0f);
        Assert.Contains(result.SilhouetteMap.CopyValues(), value => value > 0f);
        Assert.Contains(result.FocalMap.CopyValues(), value => value > 0f);
        Assert.False(result.SubjectMap.CopyValues().SequenceEqual(result.SilhouetteMap.CopyValues()));
    }

    [Fact]
    public async Task AnalyzeAsyncIsDeterministic()
    {
        RgbaImage image = CreateSubjectImage(40, 30);
        HeuristicSemanticImportanceAnalyzer analyzer = new();
        SemanticAnalysisSettings settings = CreateSensitiveSettings();

        SemanticAnalysisResult first = await analyzer.AnalyzeAsync(image, settings);
        SemanticAnalysisResult second = await analyzer.AnalyzeAsync(image, settings);

        Assert.Equal(first.ImportanceMap.CopyValues(), second.ImportanceMap.CopyValues());
        Assert.True(first.Regions.SequenceEqual(second.Regions));
    }

    [Fact]
    public async Task AnalyzeAsyncHonorsMaximumSubjectCount()
    {
        RgbaImage image = CreateMultipleSubjectImage(64, 40);
        SemanticAnalysisSettings settings = new(
            subjectThreshold: 0.25d,
            minimumSubjectAreaRatio: 0.002d,
            maximumSubjects: 1,
            smoothingRadius: 0,
            boundaryRadius: 1);

        SemanticAnalysisResult result = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(image, settings);

        Assert.Single(result.Regions, region => region.Role == SemanticRegionRole.Subject);
    }

    [Fact]
    public async Task AnalyzeAsyncReportsOrderedStages()
    {
        RecordingProgress progress = new();

        _ = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
            CreateSubjectImage(48, 36),
            CreateSensitiveSettings(),
            progress);

        Assert.Equal(SemanticAnalysisStage.Preparing, progress.Values[0].Stage);
        Assert.Contains(progress.Values, value => value.Stage == SemanticAnalysisStage.ComputingSaliency);
        Assert.Contains(progress.Values, value => value.Stage == SemanticAnalysisStage.SegmentingSubjects);
        Assert.Contains(progress.Values, value => value.Stage == SemanticAnalysisStage.BuildingSilhouettes);
        Assert.Contains(progress.Values, value => value.Stage == SemanticAnalysisStage.CombiningMaps);
        Assert.Equal(SemanticAnalysisStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public async Task AnalyzeAsyncHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
                CreateSubjectImage(32, 24),
                CreateSensitiveSettings(),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task AnalyzeAsyncUsesStableProviderIdentifier()
    {
        SemanticAnalysisResult result = await new HeuristicSemanticImportanceAnalyzer().AnalyzeAsync(
            CreateSubjectImage(32, 24),
            CreateSensitiveSettings());

        Assert.Equal(HeuristicSemanticImportanceAnalyzer.ProviderIdentifier, result.ProviderId);
        Assert.All(
            result.Regions,
            region => Assert.Equal(HeuristicSemanticImportanceAnalyzer.ProviderIdentifier, region.ProviderId));
    }

    private static SemanticAnalysisSettings CreateSensitiveSettings()
    {
        return new SemanticAnalysisSettings(
            subjectThreshold: 0.25d,
            minimumSubjectAreaRatio: 0.002d,
            maximumSubjects: 4,
            centerBias: 0.4d,
            smoothingRadius: 1,
            boundaryRadius: 2);
    }

    private static RgbaImage CreateSubjectImage(int width, int height)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, Rgba32.Opaque(90, 95, 100));
        int left = width / 4;
        int right = width - left;
        int top = height / 5;
        int bottom = height - top;
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                pixels[checked((y * width) + x)] = Rgba32.Opaque(220, 45, 35);
            }
        }

        int focalLeft = width / 2 - Math.Max(1, width / 16);
        int focalRight = width / 2 + Math.Max(1, width / 16);
        int focalTop = height / 2 - Math.Max(1, height / 16);
        int focalBottom = height / 2 + Math.Max(1, height / 16);
        for (int y = focalTop; y < focalBottom; y++)
        {
            for (int x = focalLeft; x < focalRight; x++)
            {
                pixels[checked((y * width) + x)] = Rgba32.Opaque(245, 245, 220);
            }
        }

        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private static RgbaImage CreateMultipleSubjectImage(int width, int height)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, Rgba32.Opaque(80, 80, 80));
        PaintRectangle(pixels, width, 6, 8, 22, 32, Rgba32.Opaque(230, 30, 30));
        PaintRectangle(pixels, width, 40, 10, 57, 31, Rgba32.Opaque(20, 210, 230));
        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private static void PaintRectangle(
        Rgba32[] pixels,
        int width,
        int left,
        int top,
        int right,
        int bottom,
        Rgba32 color)
    {
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                pixels[checked((y * width) + x)] = color;
            }
        }
    }

    private static RgbaImage CreateUniformImage(int width, int height, Rgba32 color)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, color);
        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private sealed class RecordingProgress : IProgress<SemanticAnalysisProgress>
    {
        public List<SemanticAnalysisProgress> Values { get; } = [];

        public void Report(SemanticAnalysisProgress value)
        {
            Values.Add(value);
        }
    }
}
