using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class HeuristicSceneBoundaryAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsyncClassifiesUniformImageAsBackground()
    {
        RgbaImage image = CreateUniformImage(40, 30, Rgba32.Opaque(120, 120, 120));
        SemanticAnalysisResult semantic = SemanticAnalysisResult.CreateEmpty(image.Size);

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            semantic,
            new SceneBoundaryAnalysisSettings(smoothingRadius: 0));

        Assert.All(result.EdgeStrengthMap.CopyValues(), value => Assert.Equal(0f, value));
        Assert.True(result.BackgroundConfidenceMap[20, 15] > 0.95f);
        Assert.True(result.UncertaintyMap[20, 15] < 0.05f);
        Assert.False(result.DirectionField[20, 15].IsDefined);
    }

    [Fact]
    public async Task AnalyzeAsyncFindsVerticalBoundaryAndVerticalTangent()
    {
        RgbaImage image = CreateSplitImage(
            48,
            36,
            vertical: true,
            Rgba32.Opaque(20, 20, 20),
            Rgba32.Opaque(235, 235, 235));

        SceneBoundaryAnalysisResult result = await AnalyzeWithoutSemanticsAsync(image);
        BoundaryVector direction = result.DirectionField[24, 18];

        Assert.True(result.EdgeStrengthMap[24, 18] > 0.5f);
        Assert.True(direction.IsDefined);
        Assert.True(Math.Abs(direction.Y) > 0.9d);
        Assert.True(Math.Abs(direction.X) < 0.2d);
    }

    [Fact]
    public async Task AnalyzeAsyncFindsHorizontalBoundaryAndHorizontalTangent()
    {
        RgbaImage image = CreateSplitImage(
            48,
            36,
            vertical: false,
            Rgba32.Opaque(20, 20, 20),
            Rgba32.Opaque(235, 235, 235));

        SceneBoundaryAnalysisResult result = await AnalyzeWithoutSemanticsAsync(image);
        BoundaryVector direction = result.DirectionField[24, 18];

        Assert.True(result.EdgeStrengthMap[24, 18] > 0.5f);
        Assert.True(direction.IsDefined);
        Assert.True(Math.Abs(direction.X) > 0.9d);
        Assert.True(Math.Abs(direction.Y) < 0.2d);
    }

    [Fact]
    public async Task AnalyzeAsyncDetectsChromaticBoundaryWithEqualLuminance()
    {
        RgbaImage image = CreateSplitImage(
            48,
            36,
            vertical: true,
            Rgba32.Opaque(255, 0, 0),
            Rgba32.Opaque(0, 76, 0));
        SceneBoundaryAnalysisSettings settings = new(
            luminanceWeight: 0d,
            colorWeight: 1d,
            edgeThreshold: 0.02d,
            importantEdgeThreshold: 0.1d,
            smoothingRadius: 0);

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            SemanticAnalysisResult.CreateEmpty(image.Size),
            settings);

        Assert.True(result.EdgeStrengthMap[24, 18] > 0.2f);
    }

    [Fact]
    public async Task AnalyzeAsyncUsesSemanticSilhouetteToPromoteSubjectBoundary()
    {
        RgbaImage image = CreateSplitImage(
            48,
            36,
            vertical: true,
            Rgba32.Opaque(60, 60, 60),
            Rgba32.Opaque(150, 150, 150));
        SemanticAnalysisResult semantic = CreateVerticalSubjectSemanticResult(image.Size, 24);
        SceneBoundaryAnalysisSettings settings = new(
            semanticBoundaryWeight: 2d,
            edgeThreshold: 0.02d,
            importantEdgeThreshold: 0.05d,
            smoothingRadius: 0);

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            semantic,
            settings);

        Assert.True(result.SubjectBoundaryMap[24, 18] > 0.3f);
        Assert.True(result.EdgeImportanceMap[24, 18] > result.EdgeImportanceMap[4, 18]);
    }

    [Fact]
    public async Task AnalyzeAsyncSeparatesInternalStructureFromSilhouette()
    {
        ImageSize size = new(48, 36);
        Rgba32[] pixels = CreateFilledPixels(size, Rgba32.Opaque(40, 40, 40));
        PaintRectangle(pixels, size, 10, 7, 38, 29, Rgba32.Opaque(210, 80, 60));
        PaintRectangle(pixels, size, 21, 12, 27, 24, Rgba32.Opaque(245, 235, 220));
        RgbaImage image = new(size, pixels);
        SemanticAnalysisResult semantic = CreateRectangularSubjectSemanticResult(
            size,
            10,
            7,
            38,
            29);

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            semantic,
            new SceneBoundaryAnalysisSettings(edgeThreshold: 0.02d, importantEdgeThreshold: 0.08d, smoothingRadius: 0));

        Assert.Contains(result.InternalStructureMap.CopyValues(), value => value > 0.05f);
        Assert.Contains(result.SubjectBoundaryMap.CopyValues(), value => value > 0.05f);
        Assert.False(result.InternalStructureMap.CopyValues().SequenceEqual(result.SubjectBoundaryMap.CopyValues()));
    }

    [Fact]
    public async Task AnalyzeAsyncProtectsAreaNearSubjectFromBackgroundClassification()
    {
        ImageSize size = new(60, 40);
        RgbaImage image = CreateUniformImage(size.Width, size.Height, Rgba32.Opaque(110, 110, 110));
        SemanticAnalysisResult semantic = CreateCentralSubjectSemanticResult(size);

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            semantic,
            new SceneBoundaryAnalysisSettings(boundaryProtectionRadius: 6, smoothingRadius: 0));

        Assert.True(result.BackgroundConfidenceMap[2, 2] > 0.7f);
        Assert.True(result.BackgroundConfidenceMap[19, 20] < result.BackgroundConfidenceMap[2, 2]);
        Assert.True(result.BackgroundConfidenceMap[30, 20] < 0.1f);
    }

    [Fact]
    public async Task AnalyzeAsyncIdentifiesFineTextureSeparately()
    {
        RgbaImage image = CreateCheckerImage(48, 36);

        SceneBoundaryAnalysisResult result = await AnalyzeWithoutSemanticsAsync(image);

        Assert.Contains(result.TextureEdgeMap.CopyValues(), value => value > 0.05f);
        Assert.All(result.SubjectBoundaryMap.CopyValues(), value => Assert.Equal(0f, value));
    }

    [Fact]
    public async Task AnalyzeAsyncIsDeterministic()
    {
        RgbaImage image = CreateSplitImage(
            40,
            30,
            vertical: true,
            Rgba32.Opaque(30, 50, 80),
            Rgba32.Opaque(220, 170, 90));
        HeuristicSceneBoundaryAnalyzer analyzer = new();
        SceneBoundaryAnalysisSettings settings = new(smoothingRadius: 1);
        SemanticAnalysisResult semantic = SemanticAnalysisResult.CreateEmpty(image.Size);

        SceneBoundaryAnalysisResult first = await analyzer.AnalyzeAsync(image, semantic, settings);
        SceneBoundaryAnalysisResult second = await analyzer.AnalyzeAsync(image, semantic, settings);

        Assert.Equal(first.EdgeImportanceMap.CopyValues(), second.EdgeImportanceMap.CopyValues());
        Assert.Equal(first.DirectionField.CopyVectors(), second.DirectionField.CopyVectors());
    }

    [Fact]
    public async Task AnalyzeAsyncReturnsEmptyResultWhenDisabled()
    {
        RgbaImage image = CreateSplitImage(
            32,
            24,
            vertical: true,
            Rgba32.Opaque(0, 0, 0),
            Rgba32.Opaque(255, 255, 255));

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            SemanticAnalysisResult.CreateEmpty(image.Size),
            new SceneBoundaryAnalysisSettings(enabled: false));

        Assert.All(result.EdgeStrengthMap.CopyValues(), value => Assert.Equal(0f, value));
        Assert.All(result.DirectionField.CopyVectors(), vector => Assert.False(vector.IsDefined));
    }

    [Fact]
    public async Task AnalyzeAsyncRejectsMismatchedSemanticDimensions()
    {
        RgbaImage image = CreateUniformImage(32, 24, Rgba32.Opaque(100, 100, 100));

        await Assert.ThrowsAsync<ArgumentException>(() => new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            SemanticAnalysisResult.CreateEmpty(new ImageSize(16, 12)),
            new SceneBoundaryAnalysisSettings()));
    }

    [Fact]
    public async Task AnalyzeAsyncReportsOrderedStages()
    {
        RecordingProgress progress = new();
        RgbaImage image = CreateSplitImage(
            32,
            24,
            vertical: true,
            Rgba32.Opaque(0, 0, 0),
            Rgba32.Opaque(255, 255, 255));

        _ = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            SemanticAnalysisResult.CreateEmpty(image.Size),
            new SceneBoundaryAnalysisSettings(),
            progress);

        Assert.Equal(SceneBoundaryAnalysisStage.Preparing, progress.Values[0].Stage);
        Assert.Contains(progress.Values, value => value.Stage == SceneBoundaryAnalysisStage.ComputingMultiscaleEdges);
        Assert.Contains(progress.Values, value => value.Stage == SceneBoundaryAnalysisStage.LinkingContours);
        Assert.Contains(progress.Values, value => value.Stage == SceneBoundaryAnalysisStage.ClassifyingBoundaries);
        Assert.Contains(progress.Values, value => value.Stage == SceneBoundaryAnalysisStage.EstimatingBackground);
        Assert.Equal(SceneBoundaryAnalysisStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public async Task AnalyzeAsyncHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        RgbaImage image = CreateUniformImage(32, 24, Rgba32.Opaque(100, 100, 100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
                image,
                SemanticAnalysisResult.CreateEmpty(image.Size),
                new SceneBoundaryAnalysisSettings(),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task AnalyzeAsyncUsesStableProviderIdentifier()
    {
        RgbaImage image = CreateUniformImage(16, 12, Rgba32.Opaque(100, 100, 100));

        SceneBoundaryAnalysisResult result = await new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            SemanticAnalysisResult.CreateEmpty(image.Size),
            new SceneBoundaryAnalysisSettings());

        Assert.Equal(HeuristicSceneBoundaryAnalyzer.ProviderIdentifier, result.ProviderId);
    }

    private static Task<SceneBoundaryAnalysisResult> AnalyzeWithoutSemanticsAsync(RgbaImage image)
    {
        return new HeuristicSceneBoundaryAnalyzer().AnalyzeAsync(
            image,
            SemanticAnalysisResult.CreateEmpty(image.Size),
            new SceneBoundaryAnalysisSettings(
                edgeThreshold: 0.02d,
                importantEdgeThreshold: 0.08d,
                smoothingRadius: 0));
    }

    private static SemanticAnalysisResult CreateVerticalSubjectSemanticResult(ImageSize size, int boundaryX)
    {
        float[] subject = new float[checked((int)size.PixelCount)];
        float[] silhouette = new float[subject.Length];
        for (int y = 0; y < size.Height; y++)
        {
            for (int x = boundaryX; x < size.Width; x++)
            {
                subject[checked((y * size.Width) + x)] = 1f;
            }

            silhouette[checked((y * size.Width) + boundaryX)] = 1f;
        }

        DetailMap zero = DetailMap.CreateUniform(size, 0f);
        DetailMap subjectMap = new(size.Width, size.Height, subject);
        DetailMap silhouetteMap = new(size.Width, size.Height, silhouette);
        return new SemanticAnalysisResult(
            zero,
            subjectMap,
            silhouetteMap,
            zero,
            subjectMap);
    }

    private static SemanticAnalysisResult CreateRectangularSubjectSemanticResult(
        ImageSize size,
        int left,
        int top,
        int right,
        int bottom)
    {
        float[] subject = new float[checked((int)size.PixelCount)];
        float[] silhouette = new float[subject.Length];
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                int index = checked((y * size.Width) + x);
                subject[index] = 1f;
                if (x == left || x == right - 1 || y == top || y == bottom - 1)
                {
                    silhouette[index] = 1f;
                }
            }
        }

        DetailMap zero = DetailMap.CreateUniform(size, 0f);
        DetailMap subjectMap = new(size.Width, size.Height, subject);
        DetailMap silhouetteMap = new(size.Width, size.Height, silhouette);
        return new SemanticAnalysisResult(
            zero,
            subjectMap,
            silhouetteMap,
            zero,
            subjectMap);
    }

    private static SemanticAnalysisResult CreateCentralSubjectSemanticResult(ImageSize size)
    {
        float[] subject = new float[checked((int)size.PixelCount)];
        float[] silhouette = new float[subject.Length];
        int left = size.Width / 3;
        int right = size.Width - left;
        int top = size.Height / 4;
        int bottom = size.Height - top;
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                int index = checked((y * size.Width) + x);
                subject[index] = 1f;
                if (x == left || x == right - 1 || y == top || y == bottom - 1)
                {
                    silhouette[index] = 1f;
                }
            }
        }

        DetailMap zero = DetailMap.CreateUniform(size, 0f);
        DetailMap subjectMap = new(size.Width, size.Height, subject);
        DetailMap silhouetteMap = new(size.Width, size.Height, silhouette);
        return new SemanticAnalysisResult(
            zero,
            subjectMap,
            silhouetteMap,
            zero,
            subjectMap);
    }

    private static RgbaImage CreateSplitImage(
        int width,
        int height,
        bool vertical,
        Rgba32 first,
        Rgba32 second)
    {
        ImageSize size = new(width, height);
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool useSecond = vertical ? x >= width / 2 : y >= height / 2;
                pixels[checked((y * width) + x)] = useSecond ? second : first;
            }
        }

        return new RgbaImage(size, pixels);
    }

    private static RgbaImage CreateCheckerImage(int width, int height)
    {
        ImageSize size = new(width, height);
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte value = (((x / 2) + (y / 2)) & 1) == 0 ? (byte)70 : (byte)180;
                pixels[checked((y * width) + x)] = Rgba32.Opaque(value, value, value);
            }
        }

        return new RgbaImage(size, pixels);
    }

    private static RgbaImage CreateUniformImage(int width, int height, Rgba32 color)
    {
        ImageSize size = new(width, height);
        return new RgbaImage(size, CreateFilledPixels(size, color));
    }

    private static Rgba32[] CreateFilledPixels(ImageSize size, Rgba32 color)
    {
        Rgba32[] pixels = new Rgba32[checked((int)size.PixelCount)];
        Array.Fill(pixels, color);
        return pixels;
    }

    private static void PaintRectangle(
        Rgba32[] pixels,
        ImageSize size,
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
                pixels[checked((y * size.Width) + x)] = color;
            }
        }
    }

    private sealed class RecordingProgress : IProgress<SceneBoundaryAnalysisProgress>
    {
        public List<SceneBoundaryAnalysisProgress> Values { get; } = [];

        public void Report(SceneBoundaryAnalysisProgress value)
        {
            Values.Add(value);
        }
    }
}
