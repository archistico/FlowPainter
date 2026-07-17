using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Analysis;

public sealed class AnalysisCoordinator
{
    private readonly object _gate = new();
    private readonly IDetailMapAnalyzer _detailAnalyzer;
    private readonly IRegionSegmentationAnalyzer _segmentationAnalyzer;
    private readonly IRegionalSceneBoundaryAnalyzer _boundaryAnalyzer;
    private long _latestGeneration;
    private AnalysisCacheKey? _currentKey;
    private AnalysisResult? _currentResult;

    public AnalysisCoordinator(
        IDetailMapAnalyzer detailAnalyzer,
        IRegionSegmentationAnalyzer segmentationAnalyzer,
        IRegionalSceneBoundaryAnalyzer boundaryAnalyzer)
    {
        ArgumentNullException.ThrowIfNull(detailAnalyzer);
        ArgumentNullException.ThrowIfNull(segmentationAnalyzer);
        ArgumentNullException.ThrowIfNull(boundaryAnalyzer);

        _detailAnalyzer = detailAnalyzer;
        _segmentationAnalyzer = segmentationAnalyzer;
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
            "Preparing regional image analysis..."));

        DetailMap structural = await _detailAnalyzer.AnalyzeAsync(
            request.Source,
            request.DetailSettings,
            CreateDetailProgress(progress),
            cancellationToken).ConfigureAwait(false);

        RegionSegmentationRequest segmentationRequest = new(
            request.Source,
            request.SegmentationSettings,
            mergeSettings: request.MergeSettings);
        RegionSegmentationResult segmentation = await _segmentationAnalyzer.AnalyzeAsync(
            segmentationRequest,
            CreateSegmentationProgress(progress),
            cancellationToken).ConfigureAwait(false);

        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.RegionRoles,
            0.62d,
            "Applying persistent region-role overrides..."));
        IReadOnlyList<RegionRoleOverride> roleOverrides = request.RegionRoleOverrides.Count > 0
            ? request.RegionRoleOverrides
            : LegacySemanticCorrectionAdapter.Convert(request.SemanticCorrections);
        RegionalStructureAnalysisResult regional = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            roleOverrides,
            request.DetailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);
        SemanticAnalysisResult semanticCompatibility = RegionalSemanticCompatibilityAdapter.Create(regional);

        SceneBoundaryAnalysisResult boundary = await _boundaryAnalyzer.AnalyzeAsync(
            request.Source,
            regional,
            request.BoundarySettings,
            CreateBoundaryProgress(progress),
            cancellationToken).ConfigureAwait(false);

        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.AutomaticDetail,
            0.86d,
            "Combining structural and regional detail evidence..."));
        DetailMap automatic = RegionalDetailMapComposer.Combine(
            structural,
            regional,
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
            regional,
            boundary,
            request.BackgroundSettings,
            CreateBackgroundProgress(progress),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        AnalysisResult result = new(
            structural,
            segmentation,
            regional,
            semanticCompatibility,
            boundary,
            automatic,
            manuallyComposed,
            backgroundSuppression);
        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.Completed,
            1d,
            "Regional image analysis completed."));
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
            basis.RegionalAnalysis,
            basis.BoundaryAnalysis,
            request.BackgroundSettings,
            CreateBackgroundProgress(progress),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        AnalysisResult result = new(
            basis.StructuralDetailMap,
            basis.RegionalSegmentation,
            basis.RegionalAnalysis,
            basis.SemanticAnalysis,
            basis.BoundaryAnalysis,
            basis.AutomaticDetailMap,
            manuallyComposed,
            backgroundSuppression);
        progress?.Report(new AnalysisPipelineProgress(
            AnalysisPipelineStage.Completed,
            1d,
            "Regional image analysis recomposed."));
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
                    MapStageFraction(0d, 0.25d, value.Fraction),
                    value.Stage switch
                    {
                        DetailAnalysisStage.Preparing => "Preparing structural analysis...",
                        DetailAnalysisStage.AnalyzingStructure => $"Analyzing image structure {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        DetailAnalysisStage.Smoothing => $"Smoothing structural map {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        DetailAnalysisStage.Completed => "Structural analysis completed.",
                        _ => "Analyzing image structure..."
                    })));
    }

    private static ForwardingProgress<RegionSegmentationProgress>? CreateSegmentationProgress(
        IProgress<AnalysisPipelineProgress>? progress)
    {
        return progress is null
            ? null
            : new ForwardingProgress<RegionSegmentationProgress>(value => progress.Report(
                new AnalysisPipelineProgress(
                    AnalysisPipelineStage.RegionalSegmentation,
                    MapStageFraction(0.25d, 0.62d, value.OverallFraction),
                    value.Stage switch
                    {
                        RegionSegmentationStage.Preparing => "Preparing SLIC regional segmentation...",
                        RegionSegmentationStage.Smoothing => "Pre-smoothing the segmentation proxy...",
                        RegionSegmentationStage.ConvertingColor => "Converting the segmentation proxy to CIELAB...",
                        RegionSegmentationStage.InitializingClusters => "Initializing deterministic SLIC clusters...",
                        RegionSegmentationStage.AssigningPixels => $"Assigning SLIC pixels, iteration {value.CompletedIterations:N0} / {value.TotalIterations:N0}",
                        RegionSegmentationStage.UpdatingClusters => "Updating SLIC cluster centroids...",
                        RegionSegmentationStage.RepairingConnectivity => "Repairing regional connectivity...",
                        RegionSegmentationStage.BuildingResult => "Calculating regional descriptors...",
                        RegionSegmentationStage.BuildingAdjacency => "Building the region adjacency graph...",
                        RegionSegmentationStage.BuildingHierarchy => "Building the regional hierarchy...",
                        RegionSegmentationStage.Completed => "Regional segmentation completed.",
                        _ => "Segmenting image regions..."
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
                    MapStageFraction(0.64d, 0.86d, value.Fraction),
                    value.Stage switch
                    {
                        SceneBoundaryAnalysisStage.Preparing => "Preparing region-aware boundary analysis...",
                        SceneBoundaryAnalysisStage.ComputingMultiscaleEdges => $"Computing multiscale edges {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.LinkingContours => $"Linking continuous contours {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.ClassifyingBoundaries => $"Combining regional and image boundaries {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.EstimatingBackground => $"Estimating regional background confidence {value.CompletedRows:N0} / {value.TotalRows:N0}",
                        SceneBoundaryAnalysisStage.SmoothingMaps => "Smoothing boundary maps...",
                        SceneBoundaryAnalysisStage.Completed => "Region-aware boundary analysis completed.",
                        _ => "Analyzing regional boundaries..."
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
                        BackgroundSuppressionStage.BuildingProtection => "Protecting regional focus, strong boundaries and uncertain areas...",
                        BackgroundSuppressionStage.EstimatingSuppression => "Estimating low-importance regional background...",
                        BackgroundSuppressionStage.SmoothingTransitions => "Smoothing regional transitions...",
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
