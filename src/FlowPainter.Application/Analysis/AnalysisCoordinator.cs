using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Analysis;

public sealed class AnalysisCoordinator
{
    private readonly object _gate = new();
    private readonly IDetailMapAnalyzer _detailAnalyzer;
    private readonly ISemanticImportanceAnalyzer _semanticAnalyzer;
    private readonly ISceneBoundaryAnalyzer _boundaryAnalyzer;
    private long _latestGeneration;
    private AnalysisCacheKey? _currentKey;
    private AnalysisResult? _currentResult;

    public AnalysisCoordinator(
        IDetailMapAnalyzer detailAnalyzer,
        ISemanticImportanceAnalyzer semanticAnalyzer,
        ISceneBoundaryAnalyzer boundaryAnalyzer)
    {
        ArgumentNullException.ThrowIfNull(detailAnalyzer);
        ArgumentNullException.ThrowIfNull(semanticAnalyzer);
        ArgumentNullException.ThrowIfNull(boundaryAnalyzer);

        _detailAnalyzer = detailAnalyzer;
        _semanticAnalyzer = semanticAnalyzer;
        _boundaryAnalyzer = boundaryAnalyzer;
    }

    public AnalysisCacheKey? CurrentKey
    {
        get
        {
            lock (_gate)
            {
                return _currentKey;
            }
        }
    }

    public AnalysisResult? CurrentResult
    {
        get
        {
            lock (_gate)
            {
                return _currentResult;
            }
        }
    }

    public AnalysisResult? GetCurrent(AnalysisCacheKey cacheKey)
    {
        ArgumentNullException.ThrowIfNull(cacheKey);
        lock (_gate)
        {
            return _currentKey == cacheKey ? _currentResult : null;
        }
    }

    public async Task<PendingAnalysis> AnalyzeAsync(
        AnalysisRequest request,
        IProgress<AnalysisPipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        long generation;
        lock (_gate)
        {
            generation = checked(_latestGeneration + 1L);
            _latestGeneration = generation;
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.Preparing,
            0d,
            "Preparing image analysis..."));

        DetailMap structural = await _detailAnalyzer.AnalyzeAsync(
            request.Source,
            request.DetailSettings,
            CreateDetailProgress(progress),
            cancellationToken).ConfigureAwait(false);
        SemanticAnalysisResult automaticSemantic = await _semanticAnalyzer.AnalyzeAsync(
            request.Source,
            request.SemanticSettings,
            CreateSemanticProgress(progress),
            cancellationToken).ConfigureAwait(false);

        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.SemanticCorrections,
            0.58d,
            "Applying persistent semantic corrections..."));
        SemanticAnalysisResult semantic = SemanticCorrectionComposer.Apply(
            automaticSemantic,
            request.SemanticCorrections,
            request.DetailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);

        SceneBoundaryAnalysisResult boundary = await _boundaryAnalyzer.AnalyzeAsync(
            request.Source,
            semantic,
            request.BoundarySettings,
            CreateBoundaryProgress(progress),
            cancellationToken).ConfigureAwait(false);

        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.AutomaticDetail,
            0.86d,
            "Combining structural and semantic detail maps..."));
        DetailMap automatic = SemanticDetailMapComposer.Combine(
            structural,
            semantic,
            request.SemanticSettings,
            cancellationToken);

        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.ManualRegions,
            0.89d,
            "Applying manual detail regions..."));
        DetailMap manuallyComposed = DetailMapComposer.ApplyRegions(
            automatic,
            request.DetailRegions,
            request.DetailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);

        BackgroundSuppressionResult backgroundSuppression = BackgroundSuppressionComposer.Compose(
            automatic,
            manuallyComposed,
            semantic,
            boundary,
            request.BackgroundSettings,
            CreateBackgroundProgress(progress),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        AnalysisResult result = new(
            structural,
            semantic,
            boundary,
            automatic,
            manuallyComposed,
            backgroundSuppression);
        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.Completed,
            1d,
            "Image analysis completed."));
        return new PendingAnalysis(generation, request.CacheKey, result);
    }

    public Task<PendingAnalysis> RecomposeAsync(
        AnalysisRequest request,
        AnalysisResult basis,
        IProgress<AnalysisPipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.AutomaticDetailMap.Size != request.Source.Size)
        {
            throw new ArgumentException(
                "The analysis basis must match the request source dimensions.",
                nameof(basis));
        }

        long generation;
        lock (_gate)
        {
            generation = checked(_latestGeneration + 1L);
            _latestGeneration = generation;
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.ManualRegions,
            0.89d,
            "Applying manual detail regions..."));
        DetailMap manuallyComposed = DetailMapComposer.ApplyRegions(
            basis.AutomaticDetailMap,
            request.DetailRegions,
            request.DetailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);
        BackgroundSuppressionResult backgroundSuppression = BackgroundSuppressionComposer.Compose(
            basis.AutomaticDetailMap,
            manuallyComposed,
            basis.SemanticAnalysis,
            basis.BoundaryAnalysis,
            request.BackgroundSettings,
            CreateBackgroundProgress(progress),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        AnalysisResult result = new(
            basis.StructuralDetailMap,
            basis.SemanticAnalysis,
            basis.BoundaryAnalysis,
            basis.AutomaticDetailMap,
            manuallyComposed,
            backgroundSuppression);
        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.Completed,
            1d,
            "Image analysis recomposed."));
        return Task.FromResult(new PendingAnalysis(generation, request.CacheKey, result));
    }

    public bool TryAdopt(
        PendingAnalysis pending,
        AnalysisCacheKey expectedKey,
        Action<AnalysisResult> adoption)
    {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(expectedKey);
        ArgumentNullException.ThrowIfNull(adoption);

        lock (_gate)
        {
            if (pending.Generation != _latestGeneration || pending.CacheKey != expectedKey)
            {
                return false;
            }

            adoption(pending.Result);
            _currentKey = pending.CacheKey;
            _currentResult = pending.Result;
            return true;
        }
    }

    public bool TryRetagCurrent(
        AnalysisCacheKey currentKey,
        AnalysisCacheKey replacementKey)
    {
        ArgumentNullException.ThrowIfNull(currentKey);
        ArgumentNullException.ThrowIfNull(replacementKey);

        lock (_gate)
        {
            if (_currentKey != currentKey || _currentResult is null)
            {
                return false;
            }

            _currentKey = replacementKey;
            return true;
        }
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _latestGeneration = checked(_latestGeneration + 1L);
            _currentKey = null;
            _currentResult = null;
        }
    }

    private static ForwardingProgress<DetailAnalysisProgress>? CreateDetailProgress(
        IProgress<AnalysisPipelineProgress>? progress)
    {
        return progress is null
            ? null
            : new ForwardingProgress<DetailAnalysisProgress>(value => progress.Report(
                new AnalysisPipelineProgress(
                    AnalysisPipelineStage.StructuralDetail,
                    MapStageFraction(0d, 0.30d, value.Fraction),
                    value.Stage switch
                    {
                        DetailAnalysisStage.Preparing => "Preparing structural analysis...",
                        DetailAnalysisStage.AnalyzingStructure => $"Analyzing image structure {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        DetailAnalysisStage.Smoothing => $"Smoothing structural map {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        DetailAnalysisStage.Completed => "Structural analysis completed.",
                        _ => "Analyzing image structure..."
                    })));
    }

    private static ForwardingProgress<SemanticAnalysisProgress>? CreateSemanticProgress(
        IProgress<AnalysisPipelineProgress>? progress)
    {
        return progress is null
            ? null
            : new ForwardingProgress<SemanticAnalysisProgress>(value => progress.Report(
                new AnalysisPipelineProgress(
                    AnalysisPipelineStage.SemanticImportance,
                    MapStageFraction(0.30d, 0.58d, value.Fraction),
                    value.Stage switch
                    {
                        SemanticAnalysisStage.Preparing => "Preparing semantic importance analysis...",
                        SemanticAnalysisStage.ComputingSaliency => $"Computing saliency {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SemanticAnalysisStage.SegmentingSubjects => "Segmenting generic subjects...",
                        SemanticAnalysisStage.BuildingSilhouettes => $"Building subject silhouettes {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SemanticAnalysisStage.CombiningMaps => "Combining semantic maps...",
                        SemanticAnalysisStage.Completed => "Semantic importance analysis completed.",
                        _ => "Analyzing semantic importance..."
                    })));
    }

    private static ForwardingProgress<SceneBoundaryAnalysisProgress>? CreateBoundaryProgress(
        IProgress<AnalysisPipelineProgress>? progress)
    {
        return progress is null
            ? null
            : new ForwardingProgress<SceneBoundaryAnalysisProgress>(value => progress.Report(
                new AnalysisPipelineProgress(
                    AnalysisPipelineStage.SceneBoundaries,
                    MapStageFraction(0.58d, 0.86d, value.Fraction),
                    value.Stage switch
                    {
                        SceneBoundaryAnalysisStage.Preparing => "Preparing scene-boundary analysis...",
                        SceneBoundaryAnalysisStage.ComputingMultiscaleEdges => $"Computing multiscale edges {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.LinkingContours => $"Linking continuous contours {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.ClassifyingBoundaries => $"Classifying important boundaries {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.EstimatingBackground => $"Estimating background confidence {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.SmoothingMaps => "Smoothing boundary maps...",
                        SceneBoundaryAnalysisStage.Completed => "Scene-boundary analysis completed.",
                        _ => "Analyzing scene boundaries..."
                    })));
    }

    private static ForwardingProgress<BackgroundSuppressionProgress>? CreateBackgroundProgress(
        IProgress<AnalysisPipelineProgress>? progress)
    {
        return progress is null
            ? null
            : new ForwardingProgress<BackgroundSuppressionProgress>(value => progress.Report(
                new AnalysisPipelineProgress(
                    AnalysisPipelineStage.BackgroundSuppression,
                    MapStageFraction(0.90d, 0.99d, value.Fraction),
                    value.Stage switch
                    {
                        BackgroundSuppressionStage.Preparing => "Preparing background suppression...",
                        BackgroundSuppressionStage.BuildingProtection => "Protecting subjects, silhouettes and uncertain areas...",
                        BackgroundSuppressionStage.EstimatingSuppression => "Estimating low-importance background...",
                        BackgroundSuppressionStage.SmoothingTransitions => "Smoothing subject-background transitions...",
                        BackgroundSuppressionStage.CombiningDetail => "Building the artistic detail field...",
                        BackgroundSuppressionStage.Completed => "Background suppression completed.",
                        _ => "Composing background suppression..."
                    })));
    }

    private static double MapStageFraction(
        double start,
        double end,
        double fraction)
    {
        if (fraction <= 0d)
        {
            return start;
        }

        if (fraction >= 1d)
        {
            return end;
        }

        return start + ((end - start) * fraction);
    }

    private sealed class ForwardingProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public ForwardingProgress(Action<T> report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Report(T value)
        {
            _report(value);
        }
    }
}
