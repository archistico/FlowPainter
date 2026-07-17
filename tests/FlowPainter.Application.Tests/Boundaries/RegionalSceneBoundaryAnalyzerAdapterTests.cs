using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class RegionalSceneBoundaryAnalyzerAdapterTests
{
    [Fact]
    public async Task AnalyzeAsyncForwardsRegionalBoundaryEvidenceThroughCompatibilityMaps()
    {
        RecordingBoundaryAnalyzer inner = new();
        RegionalSceneBoundaryAnalyzerAdapter adapter = new(inner);
        ImageSize size = new(2, 2);
        RgbaImage source = new(
            size,
            Enumerable.Repeat(new Rgba32(10, 20, 30, 255), 4).ToArray());
        DetailMap boundary = DetailMap.CreateUniform(size, 0.75f);
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        RegionalStructureAnalysisResult regional = new(
            empty,
            empty,
            boundary,
            empty,
            empty,
            empty,
            empty);

        _ = await adapter.AnalyzeAsync(
            source,
            regional,
            new SceneBoundaryAnalysisSettings(enabled: false));

        SemanticAnalysisResult recorded = Assert.IsType<SemanticAnalysisResult>(inner.LastSemanticAnalysis);
        Assert.Equal(boundary.CopyValues(), recorded.SilhouetteMap.CopyValues());
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task AnalyzeAsyncPropagatesCancellation()
    {
        RecordingBoundaryAnalyzer inner = new();
        RegionalSceneBoundaryAnalyzerAdapter adapter = new(inner);
        ImageSize size = new(1, 1);
        RgbaImage source = new(size, [new Rgba32(0, 0, 0, 255)]);
        RegionalStructureAnalysisResult regional = RegionalStructureAnalysisResult.CreateEmpty(size);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => adapter.AnalyzeAsync(
            source,
            regional,
            new SceneBoundaryAnalysisSettings(),
            cancellationToken: cancellation.Token));
    }

    private sealed class RecordingBoundaryAnalyzer : ISceneBoundaryAnalyzer
    {
        public int CallCount { get; private set; }

        public SemanticAnalysisResult? LastSemanticAnalysis { get; private set; }

        public Task<SceneBoundaryAnalysisResult> AnalyzeAsync(
            IRgbaPixelSource source,
            SemanticAnalysisResult semanticAnalysis,
            SceneBoundaryAnalysisSettings settings,
            IProgress<SceneBoundaryAnalysisProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _ = settings;
            _ = progress;
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastSemanticAnalysis = semanticAnalysis;
            return Task.FromResult(SceneBoundaryAnalysisResult.CreateEmpty(source.Size, "recording-boundary"));
        }
    }
}
