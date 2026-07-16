using FlowPainter.Application.Analysis;
using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Analysis;

public sealed class AnalysisCoordinatorTests
{
    [Fact]
    public void CacheKeyIsValueBasedForEquivalentInputs()
    {
        Guid sourceIdentity = Guid.NewGuid();
        AnalysisCacheKey first = CreateRequest(sourceIdentity: sourceIdentity).CacheKey;
        AnalysisCacheKey second = CreateRequest(sourceIdentity: sourceIdentity).CacheKey;

        Assert.Equal(first, second);
    }

    [Fact]
    public void CacheKeyChangesWithSourceSettingsAndRevisions()
    {
        Guid sourceIdentity = Guid.NewGuid();
        AnalysisCacheKey baseline = CreateRequest(sourceIdentity: sourceIdentity).CacheKey;
        AnalysisCacheKey sourceChanged = CreateRequest(sourceIdentity: Guid.NewGuid()).CacheKey;
        AnalysisCacheKey settingChanged = CreateRequest(
            sourceIdentity: sourceIdentity,
            detailSettings: new DetailAnalysisSettings(baseDetail: 0.2d)).CacheKey;
        AnalysisCacheKey revisionChanged = CreateRequest(
            sourceIdentity: sourceIdentity,
            detailRegionRevision: 2L).CacheKey;

        Assert.NotEqual(baseline, sourceChanged);
        Assert.NotEqual(baseline, settingChanged);
        Assert.NotEqual(baseline, revisionChanged);
    }

    [Fact]
    public void RequestCopiesMutableRegionCollections()
    {
        List<DetailRegion> regions =
        [
            new DetailRegion(
                "detail-1",
                new NormalizedRect(0.1d, 0.1d, 0.2d, 0.2d),
                0.8d,
                DetailRegionOrigin.Manual,
                DetailRegionIntent.IncreaseDetail)
        ];
        List<SemanticCorrectionRegion> corrections =
        [
            new SemanticCorrectionRegion(
                "correction-1",
                new NormalizedRect(0.2d, 0.2d, 0.3d, 0.3d),
                SemanticCorrectionKind.ForceSubject)
        ];

        AnalysisRequest request = CreateRequest(
            detailRegions: regions,
            semanticCorrections: corrections);
        regions.Clear();
        corrections.Clear();

        Assert.Single(request.DetailRegions);
        Assert.Single(request.SemanticCorrections);
    }

    [Fact]
    public async Task AnalyzeAsyncProducesDetachedCompleteResult()
    {
        RecordingDetailAnalyzer detail = new();
        RecordingSemanticAnalyzer semantic = new();
        RecordingBoundaryAnalyzer boundary = new();
        AnalysisCoordinator coordinator = new(detail, semantic, boundary);
        AnalysisRequest request = CreateRequest();

        PendingAnalysis pending = await coordinator.AnalyzeAsync(request);

        Assert.Equal(request.CacheKey, pending.CacheKey);
        Assert.Equal(request.Source.Size, pending.Result.StructuralDetailMap.Size);
        Assert.Equal(request.Source.Size, pending.Result.AutomaticDetailMap.Size);
        Assert.Equal(request.Source.Size, pending.Result.ManuallyComposedDetailMap.Size);
        Assert.Equal(request.Source.Size, pending.Result.BackgroundSuppression.EffectiveDetailMap.Size);
        Assert.Equal(1, detail.CallCount);
        Assert.Equal(1, semantic.CallCount);
        Assert.Equal(1, boundary.CallCount);
        Assert.Null(coordinator.CurrentKey);
    }

    [Fact]
    public async Task AnalyzeAsyncReportsMonotonicPipelineCompletion()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        List<AnalysisPipelineProgress> reported = [];
        SynchronousProgress<AnalysisPipelineProgress> progress = new(reported.Add);

        await coordinator.AnalyzeAsync(CreateRequest(), progress);

        Assert.NotEmpty(reported);
        Assert.Equal(AnalysisPipelineStage.Preparing, reported[0].Stage);
        Assert.Equal(AnalysisPipelineStage.Completed, reported[^1].Stage);
        Assert.Equal(1d, reported[^1].Fraction);
        for (int index = 1; index < reported.Count; index++)
        {
            Assert.True(reported[index].Fraction >= reported[index - 1].Fraction);
        }
    }

    [Fact]
    public async Task AnalyzeAsyncCancellationLeavesCurrentResultUnchanged()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis accepted = await coordinator.AnalyzeAsync(CreateRequest());
        Assert.True(coordinator.TryAdopt(accepted, accepted.CacheKey, _ => { }));
        AnalysisResult current = Assert.IsType<AnalysisResult>(coordinator.GetCurrent(accepted.CacheKey));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            coordinator.AnalyzeAsync(CreateRequest(), cancellationToken: cancellation.Token));

        Assert.Same(current, coordinator.GetCurrent(accepted.CacheKey));
    }

    [Fact]
    public async Task AnalyzeAsyncFailureLeavesCurrentResultUnchanged()
    {
        FailAfterFirstSemanticAnalyzer semantic = new();
        AnalysisCoordinator coordinator = new(
            new RecordingDetailAnalyzer(),
            semantic,
            new RecordingBoundaryAnalyzer());
        PendingAnalysis accepted = await coordinator.AnalyzeAsync(CreateRequest());
        Assert.True(coordinator.TryAdopt(accepted, accepted.CacheKey, _ => { }));
        AnalysisResult current = Assert.IsType<AnalysisResult>(coordinator.GetCurrent(accepted.CacheKey));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AnalyzeAsync(CreateRequest(detailRegionRevision: 2L)));

        Assert.Same(current, coordinator.GetCurrent(accepted.CacheKey));
        Assert.Equal(accepted.CacheKey, coordinator.CurrentKey);
    }

    [Fact]
    public async Task TryAdoptCommitsOnlyAfterCallbackSucceeds()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis pending = await coordinator.AnalyzeAsync(CreateRequest());
        bool callbackInvoked = false;

        bool adopted = coordinator.TryAdopt(
            pending,
            pending.CacheKey,
            result =>
            {
                callbackInvoked = true;
                Assert.Same(pending.Result, result);
            });

        Assert.True(adopted);
        Assert.True(callbackInvoked);
        Assert.Equal(pending.CacheKey, coordinator.CurrentKey);
        Assert.Same(pending.Result, coordinator.GetCurrent(pending.CacheKey));
    }

    [Fact]
    public async Task TryAdoptRejectsOlderCompletedResultAfterNewerRunStarts()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis older = await coordinator.AnalyzeAsync(CreateRequest());
        PendingAnalysis newer = await coordinator.AnalyzeAsync(CreateRequest(detailRegionRevision: 2L));
        bool olderCallback = false;

        bool olderAdopted = coordinator.TryAdopt(
            older,
            older.CacheKey,
            _ => olderCallback = true);
        bool newerAdopted = coordinator.TryAdopt(newer, newer.CacheKey, _ => { });

        Assert.False(olderAdopted);
        Assert.False(olderCallback);
        Assert.True(newerAdopted);
        Assert.Equal(newer.CacheKey, coordinator.CurrentKey);
    }

    [Fact]
    public async Task TryAdoptRejectsExpectedKeyMismatch()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis pending = await coordinator.AnalyzeAsync(CreateRequest());
        AnalysisCacheKey different = CreateRequest(detailRegionRevision: 4L).CacheKey;
        bool callbackInvoked = false;

        bool adopted = coordinator.TryAdopt(
            pending,
            different,
            _ => callbackInvoked = true);

        Assert.False(adopted);
        Assert.False(callbackInvoked);
        Assert.Null(coordinator.CurrentKey);
    }

    [Fact]
    public async Task TryAdoptCallbackFailureDoesNotPublishResult()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis pending = await coordinator.AnalyzeAsync(CreateRequest());

        Assert.Throws<InvalidOperationException>(() => coordinator.TryAdopt(
            pending,
            pending.CacheKey,
            _ => throw new InvalidOperationException("Adoption failed.")));

        Assert.Null(coordinator.CurrentKey);
        Assert.Null(coordinator.GetCurrent(pending.CacheKey));
    }

    [Fact]
    public async Task RecomposeAsyncReusesAutomaticMapsWithoutCallingAnalyzers()
    {
        RecordingDetailAnalyzer detail = new();
        RecordingSemanticAnalyzer semantic = new();
        RecordingBoundaryAnalyzer boundary = new();
        AnalysisCoordinator coordinator = new(detail, semantic, boundary);
        PendingAnalysis initial = await coordinator.AnalyzeAsync(CreateRequest());
        Assert.True(coordinator.TryAdopt(initial, initial.CacheKey, _ => { }));
        AnalysisRequest changed = CreateRequest(detailRegionRevision: 2L);

        PendingAnalysis recomposed = await coordinator.RecomposeAsync(
            changed,
            initial.Result);

        Assert.Same(initial.Result.StructuralDetailMap, recomposed.Result.StructuralDetailMap);
        Assert.Same(initial.Result.SemanticAnalysis, recomposed.Result.SemanticAnalysis);
        Assert.Same(initial.Result.BoundaryAnalysis, recomposed.Result.BoundaryAnalysis);
        Assert.Same(initial.Result.AutomaticDetailMap, recomposed.Result.AutomaticDetailMap);
        Assert.Equal(1, detail.CallCount);
        Assert.Equal(1, semantic.CallCount);
        Assert.Equal(1, boundary.CallCount);
    }

    [Fact]
    public async Task TryRetagCurrentChangesCacheIdentityWithoutReplacingResult()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis pending = await coordinator.AnalyzeAsync(CreateRequest());
        Assert.True(coordinator.TryAdopt(pending, pending.CacheKey, _ => { }));
        AnalysisCacheKey replacement = CreateRequest(detailRegionRevision: 9L).CacheKey;

        bool retagged = coordinator.TryRetagCurrent(pending.CacheKey, replacement);

        Assert.True(retagged);
        Assert.Null(coordinator.GetCurrent(pending.CacheKey));
        Assert.Same(pending.Result, coordinator.GetCurrent(replacement));
    }

    [Fact]
    public async Task InvalidateClearsCurrentAndRejectsPendingResults()
    {
        AnalysisCoordinator coordinator = CreateCoordinator();
        PendingAnalysis current = await coordinator.AnalyzeAsync(CreateRequest());
        Assert.True(coordinator.TryAdopt(current, current.CacheKey, _ => { }));
        PendingAnalysis pending = await coordinator.AnalyzeAsync(CreateRequest(detailRegionRevision: 3L));

        coordinator.Invalidate();

        Assert.Null(coordinator.CurrentKey);
        Assert.Null(coordinator.GetCurrent(current.CacheKey));
        Assert.False(coordinator.TryAdopt(pending, pending.CacheKey, _ => { }));
    }

    private static AnalysisCoordinator CreateCoordinator()
    {
        return new AnalysisCoordinator(
            new RecordingDetailAnalyzer(),
            new RecordingSemanticAnalyzer(),
            new RecordingBoundaryAnalyzer());
    }

    private static AnalysisRequest CreateRequest(
        Guid? sourceIdentity = null,
        DetailAnalysisSettings? detailSettings = null,
        IReadOnlyList<DetailRegion>? detailRegions = null,
        IReadOnlyList<SemanticCorrectionRegion>? semanticCorrections = null,
        long detailRegionRevision = 1L,
        long semanticCorrectionRevision = 1L)
    {
        ImageSize size = new(4, 3);
        Rgba32[] pixels = Enumerable.Repeat(new Rgba32(40, 80, 120, 255), (int)size.PixelCount).ToArray();
        return new AnalysisRequest(
            new RgbaImage(size, pixels),
            sourceIdentity ?? Guid.NewGuid(),
            detailSettings ?? new DetailAnalysisSettings(),
            new DetailInfluenceSettings(),
            new SemanticAnalysisSettings(enabled: false),
            new SceneBoundaryAnalysisSettings(enabled: false),
            new BackgroundSuppressionSettings(enabled: false),
            detailRegions,
            semanticCorrections,
            detailRegionRevision,
            semanticCorrectionRevision);
    }

    private sealed class RecordingDetailAnalyzer : IDetailMapAnalyzer
    {
        public int CallCount { get; private set; }

        public Task<DetailMap> AnalyzeAsync(
            IRgbaPixelSource source,
            DetailAnalysisSettings settings,
            IProgress<DetailAnalysisProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _ = settings;
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            progress?.Report(new DetailAnalysisProgress(
                DetailAnalysisStage.Completed,
                source.Size.Height,
                source.Size.Height,
                1d));
            return Task.FromResult(DetailMap.CreateUniform(source.Size, 0.25f));
        }
    }

    private sealed class RecordingSemanticAnalyzer : ISemanticImportanceAnalyzer
    {
        public int CallCount { get; private set; }

        public Task<SemanticAnalysisResult> AnalyzeAsync(
            IRgbaPixelSource source,
            SemanticAnalysisSettings settings,
            IProgress<SemanticAnalysisProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _ = settings;
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            progress?.Report(new SemanticAnalysisProgress(
                SemanticAnalysisStage.Completed,
                source.Size.Height,
                source.Size.Height,
                1d));
            return Task.FromResult(SemanticAnalysisResult.CreateEmpty(source.Size, "test-semantic"));
        }
    }

    private sealed class FailAfterFirstSemanticAnalyzer : ISemanticImportanceAnalyzer
    {
        private int _callCount;

        public Task<SemanticAnalysisResult> AnalyzeAsync(
            IRgbaPixelSource source,
            SemanticAnalysisSettings settings,
            IProgress<SemanticAnalysisProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _ = settings;
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;
            if (_callCount > 1)
            {
                throw new InvalidOperationException("Semantic analysis failed.");
            }

            progress?.Report(new SemanticAnalysisProgress(
                SemanticAnalysisStage.Completed,
                source.Size.Height,
                source.Size.Height,
                1d));
            return Task.FromResult(SemanticAnalysisResult.CreateEmpty(source.Size, "test-semantic"));
        }
    }

    private sealed class RecordingBoundaryAnalyzer : ISceneBoundaryAnalyzer
    {
        public int CallCount { get; private set; }

        public Task<SceneBoundaryAnalysisResult> AnalyzeAsync(
            IRgbaPixelSource source,
            SemanticAnalysisResult semanticAnalysis,
            SceneBoundaryAnalysisSettings settings,
            IProgress<SceneBoundaryAnalysisProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _ = semanticAnalysis;
            _ = settings;
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            progress?.Report(new SceneBoundaryAnalysisProgress(
                SceneBoundaryAnalysisStage.Completed,
                source.Size.Height,
                source.Size.Height,
                1d));
            return Task.FromResult(SceneBoundaryAnalysisResult.CreateEmpty(source.Size, "test-boundary"));
        }
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public SynchronousProgress(Action<T> report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Report(T value)
        {
            _report(value);
        }
    }
}
