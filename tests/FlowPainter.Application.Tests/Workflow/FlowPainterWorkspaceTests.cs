using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Workflow;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class FlowPainterWorkspaceTests
{
    [Fact]
    public void ConstructorCreatesCleanIdleWorkspace()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();

        Assert.False(workspace.IsDirty);
        Assert.False(workspace.HasSource);
        Assert.False(workspace.CanRender);
        Assert.False(workspace.Operation.IsBusy);
        Assert.Equal(0L, workspace.DetailRegionRevision);
        Assert.Equal(0L, workspace.SemanticCorrectionRevision);
    }

    [Fact]
    public void RegionRevisionAdvancesOnlyForSuccessfulChanges()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());
        long loadedRevision = workspace.DetailRegionRevision;

        DetailRegion first = workspace.AddRegion(
            CreateBounds(),
            0.8d,
            DetailRegionIntent.IncreaseDetail);
        Assert.Equal(loadedRevision + 1L, workspace.DetailRegionRevision);

        workspace.UpdateRegion(
            first.Id,
            new NormalizedRect(0.2d, 0.2d, 0.5d, 0.5d),
            0.6d,
            DetailRegionIntent.ReduceDetail,
            "Background");
        Assert.Equal(loadedRevision + 2L, workspace.DetailRegionRevision);

        Assert.False(workspace.MoveRegionEarlier(first.Id));
        Assert.Equal(loadedRevision + 2L, workspace.DetailRegionRevision);

        Assert.True(workspace.RemoveRegion(first.Id));
        Assert.Equal(loadedRevision + 3L, workspace.DetailRegionRevision);

        Assert.False(workspace.RemoveRegion(first.Id));
        workspace.ClearRegions();
        Assert.Equal(loadedRevision + 3L, workspace.DetailRegionRevision);
    }

    [Fact]
    public void SemanticCorrectionRevisionAdvancesOnlyForSuccessfulChanges()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());
        long loadedRevision = workspace.SemanticCorrectionRevision;

        SemanticCorrectionRegion correction = workspace.AddSemanticCorrection(
            CreateBounds(),
            SemanticCorrectionKind.ForceSubject,
            sourceSemanticRegionId: "semantic-subject-01");
        Assert.Equal(loadedRevision + 1L, workspace.SemanticCorrectionRevision);

        SemanticCorrectionRegion replacement = workspace.AddSemanticCorrection(
            CreateBounds(),
            SemanticCorrectionKind.ForceBackground,
            sourceSemanticRegionId: "semantic-subject-01");
        Assert.Equal(correction.Id, replacement.Id);
        Assert.Equal(loadedRevision + 2L, workspace.SemanticCorrectionRevision);

        Assert.True(workspace.RemoveSemanticCorrection(correction.Id));
        Assert.Equal(loadedRevision + 3L, workspace.SemanticCorrectionRevision);

        Assert.False(workspace.RemoveSemanticCorrection(correction.Id));
        workspace.ClearSemanticCorrections();
        Assert.Equal(loadedRevision + 3L, workspace.SemanticCorrectionRevision);
    }

    [Fact]
    public void ReplacingSourceOrProjectAdvancesBothAnalysisRevisions()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();

        workspace.SetSource("first.png");
        Assert.Equal(1L, workspace.DetailRegionRevision);
        Assert.Equal(1L, workspace.SemanticCorrectionRevision);

        workspace.LoadProject(CreateProject());
        Assert.Equal(2L, workspace.DetailRegionRevision);
        Assert.Equal(2L, workspace.SemanticCorrectionRevision);
    }

    [Fact]
    public void LoadProjectValidationFailureLeavesWorkspaceUnchanged()
    {
        DetailRegion originalRegion = new(
            "manual-0001",
            CreateBounds(),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject(detailRegions: [originalRegion]), "original.flowpainter.json");
        workspace.SetSeed(99UL);
        long detailRevision = workspace.DetailRegionRevision;
        long correctionRevision = workspace.SemanticCorrectionRevision;
        DetailRegion invalidRegion = new(
            "manual-0002",
            new NormalizedRect(0d, 0d, 0.001d, 0.5d),
            0.5d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.ReduceDetail);
        FlowPainterProject invalidProject = new(
            "Invalid",
            "replacement.png",
            123UL,
            new FlowPainterSettings(),
            detailRegions: [invalidRegion]);

        Assert.Throws<ArgumentOutOfRangeException>(() => workspace.LoadProject(
            invalidProject,
            "replacement.flowpainter.json"));

        Assert.Equal("Project", workspace.ProjectName);
        Assert.Equal(Path.GetFullPath("original.flowpainter.json"), workspace.ProjectPath);
        Assert.Equal("source.png", workspace.SourcePath);
        Assert.Equal(99UL, workspace.Seed);
        Assert.True(workspace.IsDirty);
        Assert.Same(originalRegion, Assert.Single(workspace.Regions.Regions));
        Assert.Empty(workspace.SemanticCorrections.Regions);
        Assert.Equal(detailRevision, workspace.DetailRegionRevision);
        Assert.Equal(correctionRevision, workspace.SemanticCorrectionRevision);
    }

    [Fact]
    public void PreparedProjectLoadAdoptsValidatedProjectAndNormalizedPath()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        FlowPainterProject project = CreateProject(seed: 123UL);
        WorkspaceProjectCandidate candidate = FlowPainterWorkspace.PrepareProjectLoad(
            project,
            "prepared.flowpainter.json");

        workspace.LoadProject(candidate);

        Assert.Equal("Project", workspace.ProjectName);
        Assert.Equal(Path.GetFullPath("prepared.flowpainter.json"), workspace.ProjectPath);
        Assert.Equal("source.png", workspace.SourcePath);
        Assert.Equal(123UL, workspace.Seed);
        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void RestoreEditStateRestoresCollectionsDirtyFlagAndChangedRevisions()
    {
        DetailRegion originalRegion = new(
            "manual-0001",
            CreateBounds(),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);
        SemanticCorrectionRegion originalCorrection = new(
            "semantic-correction-0001",
            CreateBounds(),
            SemanticCorrectionKind.ForceSubject);
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject(
            detailRegions: [originalRegion],
            semanticCorrections: [originalCorrection]));
        WorkspaceEditSnapshot snapshot = workspace.CaptureEditState();
        long detailRevision = workspace.DetailRegionRevision;
        long correctionRevision = workspace.SemanticCorrectionRevision;

        workspace.RemoveRegion(originalRegion.Id);
        workspace.AddSemanticCorrection(
            CreateBounds(),
            SemanticCorrectionKind.ForceBackground,
            sourceSemanticRegionId: "semantic-subject-02");
        workspace.RestoreEditState(snapshot);

        Assert.False(workspace.IsDirty);
        Assert.Same(originalRegion, Assert.Single(workspace.Regions.Regions));
        Assert.Same(originalCorrection, Assert.Single(workspace.SemanticCorrections.Regions));
        Assert.Equal(detailRevision + 2L, workspace.DetailRegionRevision);
        Assert.Equal(correctionRevision + 2L, workspace.SemanticCorrectionRevision);
    }

    [Fact]
    public void RestoreEditStateDoesNotAdvanceUnchangedRevisions()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());
        WorkspaceEditSnapshot snapshot = workspace.CaptureEditState();
        long detailRevision = workspace.DetailRegionRevision;
        long correctionRevision = workspace.SemanticCorrectionRevision;

        workspace.RestoreEditState(snapshot);

        Assert.Equal(detailRevision, workspace.DetailRegionRevision);
        Assert.Equal(correctionRevision, workspace.SemanticCorrectionRevision);
    }

    [Fact]
    public void SetSourceMarksWorkspaceDirtyAndClearsRegions()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.SetSource("first.png");
        workspace.AddRegion(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        workspace.AddSemanticCorrection(CreateBounds(), SemanticCorrectionKind.ForceSubject);

        workspace.SetSource("second.png");

        Assert.True(workspace.IsDirty);
        Assert.Equal("second.png", workspace.SourcePath);
        Assert.Empty(workspace.Regions.Regions);
        Assert.Empty(workspace.SemanticCorrections.Regions);
    }

    [Fact]
    public void SetSourceStartsNewProjectAssociation()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(
            CreateProject(),
            Path.Combine(Path.GetTempPath(), "existing.flowpainter.json"));

        workspace.SetSource(Path.Combine("images", "new-source.png"));

        Assert.Null(workspace.ProjectPath);
        Assert.Null(workspace.ProjectName);
        Assert.Equal("new-source", workspace.CreateProject().Name);
    }

    [Fact]
    public void SetSourceRejectsEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => CreateWorkspace().SetSource("  "));
    }

    [Fact]
    public void SetSeedMarksDirtyOnlyWhenChanged()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();

        workspace.SetSeed(10UL);
        FlowPainterProject project = CreateProject(seed: 10UL);
        workspace.LoadProject(project);
        workspace.SetSeed(10UL);

        Assert.False(workspace.IsDirty);
        workspace.SetSeed(11UL);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void SetSettingsMarksDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        FlowPainterProject project = CreateProject();
        workspace.LoadProject(project);

        workspace.SetSettings(new FlowPainterSettings(strokeCount: 1234));

        Assert.True(workspace.IsDirty);
        Assert.Equal(1234, workspace.Settings.StrokeCount);
    }

    [Fact]
    public void SetPreviewMarksDirtyOnlyWhenQualityChanges()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        workspace.SetPreview(new PreviewSettings(PreviewQuality.Standard));
        Assert.False(workspace.IsDirty);

        workspace.SetPreview(new PreviewSettings(PreviewQuality.High));
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void SetFinalRenderMarksDirtyOnlyWhenChanged()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        workspace.SetFinalRender(new FinalRenderSettings());
        Assert.False(workspace.IsDirty);

        workspace.SetFinalRender(new FinalRenderSettings(8000, RasterImageFormat.Jpeg, 85));
        Assert.True(workspace.IsDirty);
        Assert.Equal(8000, workspace.FinalRender.MaximumDimension);
    }

    [Fact]
    public void EquivalentFlowSettingsDoNotMarkWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        workspace.SetSettings(new FlowPainterSettings());

        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void EquivalentPrimitiveSettingsDoNotMarkWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        workspace.SetPrimitiveGeneration(new PrimitiveGenerationSettings());

        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void EquivalentHybridSettingsDoNotMarkWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        workspace.SetHybridGeneration(new HybridGenerationSettings());

        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void RegionOperationsMarkWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        DetailRegion region = workspace.AddRegion(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        Assert.True(workspace.IsDirty);

        workspace.LoadProject(CreateProject(detailRegions: [region]));
        workspace.UpdateRegion(region.Id, new NormalizedRect(0.2d, 0.2d, 0.5d, 0.5d), 0.5d, DetailRegionIntent.ReduceDetail, "Background");
        Assert.True(workspace.IsDirty);

        workspace.LoadProject(CreateProject(detailRegions: [region]));
        Assert.True(workspace.RemoveRegion(region.Id));
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void SemanticCorrectionOperationsMarkWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        SemanticCorrectionRegion correction = workspace.AddSemanticCorrection(
            CreateBounds(),
            SemanticCorrectionKind.ForcePrimarySubject,
            "Main subject",
            "semantic-subject-01");
        Assert.True(workspace.IsDirty);

        workspace.LoadProject(CreateProject(semanticCorrections: [correction]));
        Assert.True(workspace.RemoveSemanticCorrection(correction.Id));
        Assert.True(workspace.IsDirty);

        workspace.LoadProject(CreateProject(semanticCorrections: [correction]));
        workspace.ClearSemanticCorrections();
        Assert.True(workspace.IsDirty);
        Assert.Empty(workspace.SemanticCorrections.Regions);
    }

    [Fact]
    public void RemoveLastRegionMarksWorkspaceDirtyWhenRegionExists()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject(detailRegions:
        [
            new DetailRegion(
                "manual-0001",
                CreateBounds(),
                0.8d,
                DetailRegionOrigin.Manual,
                DetailRegionIntent.IncreaseDetail)
        ]));

        DetailRegion? removed = workspace.RemoveLastRegion();

        Assert.NotNull(removed);
        Assert.True(workspace.IsDirty);
        Assert.Empty(workspace.Regions.Regions);
    }

    [Fact]
    public void RegionReorderingMarksWorkspaceDirtyOnlyWhenOrderChanges()
    {
        DetailRegion first = new(
            "manual-0001",
            CreateBounds(),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);
        DetailRegion second = new(
            "manual-0002",
            new NormalizedRect(0.5d, 0.5d, 0.8d, 0.8d),
            0.6d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.ReduceDetail);
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject(detailRegions: [first, second]));

        Assert.False(workspace.MoveRegionEarlier(first.Id));
        Assert.False(workspace.IsDirty);
        Assert.True(workspace.MoveRegionLater(first.Id));
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void ClearRegionsDoesNotMarkCleanEmptyWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();

        workspace.ClearRegions();

        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void CreateProjectRequiresSource()
    {
        Assert.Throws<InvalidOperationException>(() => CreateWorkspace().CreateProject());
    }

    [Fact]
    public void CreateProjectCapturesCurrentState()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.SetSource("portrait.png");
        workspace.SetSeed(123UL);
        workspace.SetPreview(new PreviewSettings(PreviewQuality.High));
        workspace.AddRegion(CreateBounds(), 0.7d, DetailRegionIntent.IncreaseDetail, "Face");
        workspace.AddSemanticCorrection(
            CreateBounds(),
            SemanticCorrectionKind.ForcePrimarySubject,
            "Portrait subject");

        FlowPainterProject project = workspace.CreateProject("Portrait study");

        Assert.Equal("Portrait study", project.Name);
        Assert.Equal("portrait.png", project.SourcePath);
        Assert.Equal(123UL, project.Seed);
        Assert.Equal(PreviewQuality.High, project.Preview.Quality);
        Assert.Equal(FinalRenderSettings.DefaultMaximumDimension, project.FinalRender.MaximumDimension);
        Assert.Single(project.DetailRegions);
        Assert.Single(project.SemanticCorrections);
    }

    [Fact]
    public void CreateProjectUsesSourceFileNameAsFallbackName()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.SetSource(Path.Combine("images", "portrait.png"));

        Assert.Equal("portrait", workspace.CreateProject().Name);
    }

    [Fact]
    public void LoadProjectReplacesStateAndMarksClean()
    {
        DetailRegion region = new(
            "manual-0009",
            CreateBounds(),
            0.7d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Eyes");
        SemanticCorrectionRegion correction = new(
            "semantic-correction-0004",
            CreateBounds(),
            SemanticCorrectionKind.ForceSubject,
            "Face subject");
        FlowPainterProject project = CreateProject(
            seed: 99UL,
            detailRegions: [region],
            semanticCorrections: [correction]);
        FlowPainterWorkspace workspace = CreateWorkspace();

        workspace.LoadProject(project, Path.Combine(Path.GetTempPath(), "project.flowpainter.json"));

        Assert.False(workspace.IsDirty);
        Assert.Equal(project.Name, workspace.ProjectName);
        Assert.Equal(project.SourcePath, workspace.SourcePath);
        Assert.Equal(99UL, workspace.Seed);
        Assert.Single(workspace.Regions.Regions);
        Assert.Single(workspace.SemanticCorrections.Regions);
        Assert.Equal(project.FinalRender.MaximumDimension, workspace.FinalRender.MaximumDimension);
        Assert.NotNull(workspace.ProjectPath);
    }

    [Fact]
    public void MarkSavedStoresFullPathAndMarksClean()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.SetSource("source.png");
        string path = Path.Combine(Path.GetTempPath(), "project.flowpainter.json");

        workspace.MarkSaved(path, "Saved project");

        Assert.False(workspace.IsDirty);
        Assert.Equal(Path.GetFullPath(path), workspace.ProjectPath);
        Assert.Equal("Saved project", workspace.ProjectName);
    }

    [Fact]
    public void BeginOperationSetsBusyState()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();

        workspace.BeginOperation(WorkspaceOperationKind.LoadingImage, "Loading image");

        Assert.True(workspace.Operation.IsBusy);
        Assert.True(workspace.Operation.CanCancel);
        Assert.Equal(WorkspaceOperationKind.LoadingImage, workspace.Operation.Kind);
    }

    [Fact]
    public void BeginOperationRejectsConcurrentOperation()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.BeginOperation(WorkspaceOperationKind.LoadingImage, "Loading image");

        Assert.Throws<InvalidOperationException>(() => workspace.BeginOperation(
            WorkspaceOperationKind.RenderingPreview,
            "Rendering"));
    }

    [Fact]
    public void ReportOperationUpdatesProgressAndPreservesKind()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.BeginOperation(WorkspaceOperationKind.AnalyzingDetail, "Starting");

        workspace.ReportOperation(0.5d, "Halfway");

        Assert.Equal(WorkspaceOperationKind.AnalyzingDetail, workspace.Operation.Kind);
        Assert.Equal(0.5d, workspace.Operation.Progress);
        Assert.Equal("Halfway", workspace.Operation.Message);
    }

    [Fact]
    public void ReportOperationRequiresRunningOperation()
    {
        Assert.Throws<InvalidOperationException>(() => CreateWorkspace().ReportOperation(0.5d, "Halfway"));
    }

    [Fact]
    public void CompleteAndResetOperationReturnToIdleState()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.BeginOperation(WorkspaceOperationKind.RenderingPreview, "Rendering");

        workspace.CompleteOperation("Rendered");
        Assert.False(workspace.Operation.IsBusy);
        Assert.Equal(1d, workspace.Operation.Progress);
        Assert.Equal("Rendered", workspace.Operation.Message);

        workspace.ResetOperation();
        Assert.Same(WorkspaceOperationState.Idle, workspace.Operation);
    }

    [Fact]
    public void ValidationMessagesExposeReadOnlyView()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.AddValidation(new WorkspaceValidationMessage("source", "Missing"));
        IList<WorkspaceValidationMessage> messages = Assert.IsAssignableFrom<IList<WorkspaceValidationMessage>>(
            workspace.ValidationMessages);

        Assert.Throws<NotSupportedException>(() => messages.Clear());
        Assert.Single(workspace.ValidationMessages);
    }

    [Fact]
    public void ValidationMessagesReplaceMatchingCodeIgnoringCase()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.AddValidation(new WorkspaceValidationMessage("source", "Missing"));

        workspace.AddValidation(new WorkspaceValidationMessage("SOURCE", "Not found", ValidationSeverity.Warning));

        WorkspaceValidationMessage message = Assert.Single(workspace.ValidationMessages);
        Assert.Equal("Not found", message.Message);
        Assert.Equal(ValidationSeverity.Warning, message.Severity);
    }

    [Fact]
    public void ClearValidationRemovesAllMessages()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.AddValidation(new WorkspaceValidationMessage("source", "Missing"));

        workspace.ClearValidation();

        Assert.Empty(workspace.ValidationMessages);
    }

    private static FlowPainterWorkspace CreateWorkspace()
    {
        return new FlowPainterWorkspace(10UL, new FlowPainterSettings());
    }

    private static FlowPainterProject CreateProject(
        ulong seed = 10UL,
        IReadOnlyList<DetailRegion>? detailRegions = null,
        IReadOnlyList<SemanticCorrectionRegion>? semanticCorrections = null)
    {
        return new FlowPainterProject(
            "Project",
            "source.png",
            seed,
            new FlowPainterSettings(),
            detailRegions: detailRegions,
            semanticCorrections: semanticCorrections);
    }

    private static NormalizedRect CreateBounds()
    {
        return new NormalizedRect(0.1d, 0.1d, 0.4d, 0.5d);
    }
}
