using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Projects;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Workflow;

public sealed class FlowPainterWorkspace
{
    private readonly List<WorkspaceValidationMessage> _validationMessages = [];
    private readonly IReadOnlyList<WorkspaceValidationMessage> _readOnlyValidationMessages;

    public FlowPainterWorkspace(
        ulong seed,
        FlowPainterSettings settings,
        PreviewSettings? preview = null,
        FinalRenderSettings? finalRender = null,
        GenerativeMode mode = GenerativeMode.FlowPainting,
        PrimitiveGenerationSettings? primitiveGeneration = null,
        HybridGenerationSettings? hybridGeneration = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown generative mode.");
        }

        _readOnlyValidationMessages = _validationMessages.AsReadOnly();
        Seed = seed;
        Settings = settings;
        Preview = preview ?? new PreviewSettings();
        FinalRender = finalRender ?? new FinalRenderSettings();
        Mode = mode;
        PrimitiveGeneration = primitiveGeneration ?? new PrimitiveGenerationSettings();
        HybridGeneration = hybridGeneration ?? new HybridGenerationSettings();
        Regions = new DetailRegionEditor();
        SemanticCorrections = new SemanticCorrectionRegionEditor();
        Operation = WorkspaceOperationState.Idle;
    }

    public string? ProjectPath { get; private set; }

    public string? ProjectName { get; private set; }

    public string? SourcePath { get; private set; }

    public ulong Seed { get; private set; }

    public FlowPainterSettings Settings { get; private set; }

    public PreviewSettings Preview { get; private set; }

    public FinalRenderSettings FinalRender { get; private set; }

    public GenerativeMode Mode { get; private set; }

    public PrimitiveGenerationSettings PrimitiveGeneration { get; private set; }

    public HybridGenerationSettings HybridGeneration { get; private set; }

    public DetailRegionEditor Regions { get; }

    public SemanticCorrectionRegionEditor SemanticCorrections { get; }

    public WorkspaceOperationState Operation { get; private set; }

    public IReadOnlyList<WorkspaceValidationMessage> ValidationMessages => _readOnlyValidationMessages;

    public bool IsDirty { get; private set; }

    public long DetailRegionRevision { get; private set; }

    public long SemanticCorrectionRevision { get; private set; }

    public bool HasSource => !string.IsNullOrWhiteSpace(SourcePath);

    public bool CanSaveProject => HasSource;

    public bool CanRender => HasSource && !Operation.IsBusy;

    public void MarkProjectEdited()
    {
        if (HasSource)
        {
            MarkDirty();
        }
    }

    public void SetSource(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("A source path is required.", nameof(sourcePath));
        }

        SourcePath = sourcePath.Trim();
        ProjectPath = null;
        ProjectName = null;
        Regions.Clear();
        SemanticCorrections.Clear();
        AdvanceDetailRegionRevision();
        AdvanceSemanticCorrectionRevision();
        MarkDirty();
        ClearValidation();
    }

    public void SetSeed(ulong seed)
    {
        if (Seed == seed)
        {
            return;
        }

        Seed = seed;
        MarkDirty();
    }

    public void SetSettings(FlowPainterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ProjectSettingsEquality.AreEquivalent(Settings, settings))
        {
            return;
        }

        Settings = settings;
        MarkDirty();
    }

    public void SetPreview(PreviewSettings preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (Preview.Quality == preview.Quality)
        {
            return;
        }

        Preview = preview;
        MarkDirty();
    }


    public void SetFinalRender(FinalRenderSettings finalRender)
    {
        ArgumentNullException.ThrowIfNull(finalRender);
        if (FinalRender.MaximumDimension == finalRender.MaximumDimension
            && FinalRender.Format == finalRender.Format
            && FinalRender.JpegQuality == finalRender.JpegQuality)
        {
            return;
        }

        FinalRender = finalRender;
        MarkDirty();
    }


    public void SetGenerativeMode(GenerativeMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown generative mode.");
        }

        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        MarkDirty();
    }

    public void SetPrimitiveGeneration(PrimitiveGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ProjectSettingsEquality.AreEquivalent(PrimitiveGeneration, settings))
        {
            return;
        }

        PrimitiveGeneration = settings;
        MarkDirty();
    }

    public void SetHybridGeneration(HybridGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ProjectSettingsEquality.AreEquivalent(HybridGeneration, settings))
        {
            return;
        }

        HybridGeneration = settings;
        MarkDirty();
    }

    public DetailRegion AddRegion(
        FlowPainter.Domain.Geometry.NormalizedRect bounds,
        double strength,
        DetailRegionIntent intent,
        string? label = null)
    {
        DetailRegion region = Regions.Add(bounds, strength, intent, label);
        AdvanceDetailRegionRevision();
        MarkDirty();
        return region;
    }

    public DetailRegion UpdateRegion(
        string id,
        FlowPainter.Domain.Geometry.NormalizedRect bounds,
        double strength,
        DetailRegionIntent intent,
        string? label)
    {
        DetailRegion region = Regions.Update(id, bounds, strength, intent, label);
        AdvanceDetailRegionRevision();
        MarkDirty();
        return region;
    }

    public bool RemoveRegion(string id)
    {
        bool removed = Regions.Remove(id);
        if (removed)
        {
            AdvanceDetailRegionRevision();
            MarkDirty();
        }

        return removed;
    }

    public DetailRegion? RemoveLastRegion()
    {
        DetailRegion? removed = Regions.RemoveLast();
        if (removed is not null)
        {
            AdvanceDetailRegionRevision();
            MarkDirty();
        }

        return removed;
    }

    public bool MoveRegionEarlier(string id)
    {
        bool moved = Regions.MoveEarlier(id);
        if (moved)
        {
            AdvanceDetailRegionRevision();
            MarkDirty();
        }

        return moved;
    }

    public bool MoveRegionLater(string id)
    {
        bool moved = Regions.MoveLater(id);
        if (moved)
        {
            AdvanceDetailRegionRevision();
            MarkDirty();
        }

        return moved;
    }

    public void ClearRegions()
    {
        if (Regions.Count == 0)
        {
            return;
        }

        Regions.Clear();
        AdvanceDetailRegionRevision();
        MarkDirty();
    }

    public SemanticCorrectionRegion AddSemanticCorrection(
        FlowPainter.Domain.Geometry.NormalizedRect bounds,
        SemanticCorrectionKind kind,
        string? label = null,
        string? sourceSemanticRegionId = null)
    {
        SemanticCorrectionRegion correction = SemanticCorrections.Add(
            bounds,
            kind,
            label,
            sourceSemanticRegionId);
        AdvanceSemanticCorrectionRevision();
        MarkDirty();
        return correction;
    }

    public bool RemoveSemanticCorrection(string id)
    {
        bool removed = SemanticCorrections.Remove(id);
        if (removed)
        {
            AdvanceSemanticCorrectionRevision();
            MarkDirty();
        }

        return removed;
    }

    public void ClearSemanticCorrections()
    {
        if (SemanticCorrections.Count == 0)
        {
            return;
        }

        SemanticCorrections.Clear();
        AdvanceSemanticCorrectionRevision();
        MarkDirty();
    }

    public WorkspaceEditSnapshot CaptureEditState()
    {
        return new WorkspaceEditSnapshot(
            Regions.Regions,
            SemanticCorrections.Regions,
            IsDirty);
    }

    public void RestoreEditState(WorkspaceEditSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!Regions.Regions.SequenceEqual(snapshot.DetailRegions))
        {
            Regions.ReplaceAll(snapshot.DetailRegions);
            AdvanceDetailRegionRevision();
        }

        if (!SemanticCorrections.Regions.SequenceEqual(snapshot.SemanticCorrections))
        {
            SemanticCorrections.ReplaceAll(snapshot.SemanticCorrections);
            AdvanceSemanticCorrectionRevision();
        }

        IsDirty = snapshot.IsDirty;
    }

    public FlowPainterProject CreateProject(string? projectName = null)
    {
        if (!HasSource)
        {
            throw new InvalidOperationException("Open a source image before saving a project.");
        }

        string resolvedName = ResolveProjectName(projectName);
        return new FlowPainterProject(
            resolvedName,
            SourcePath!,
            Seed,
            Settings,
            Preview,
            Regions.Regions,
            FinalRender,
            Mode,
            PrimitiveGeneration,
            HybridGeneration,
            SemanticCorrections.Regions);
    }

    public void LoadProject(FlowPainterProject project, string? projectPath = null)
    {
        LoadProject(PrepareProjectLoad(project, projectPath));
    }

    public static WorkspaceProjectCandidate PrepareProjectLoad(
        FlowPainterProject project,
        string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        DetailRegionEditor validatedRegions = new();
        validatedRegions.ReplaceAll(project.DetailRegions);
        SemanticCorrectionRegionEditor validatedCorrections = new();
        validatedCorrections.ReplaceAll(project.SemanticCorrections);
        return new WorkspaceProjectCandidate(project, NormalizeOptionalPath(projectPath));
    }

    public void LoadProject(WorkspaceProjectCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        FlowPainterProject project = candidate.Project;
        ProjectName = project.Name;
        ProjectPath = candidate.ProjectPath;
        SourcePath = project.SourcePath;
        Seed = project.Seed;
        Settings = project.Settings;
        Preview = project.Preview;
        FinalRender = project.FinalRender;
        Mode = project.Mode;
        PrimitiveGeneration = project.PrimitiveGeneration;
        HybridGeneration = project.HybridGeneration;
        Regions.ReplaceAll(project.DetailRegions);
        SemanticCorrections.ReplaceAll(project.SemanticCorrections);
        AdvanceDetailRegionRevision();
        AdvanceSemanticCorrectionRevision();
        IsDirty = false;
        ClearValidation();
        Operation = WorkspaceOperationState.Idle;
    }

    public void MarkSaved(string projectPath, string? projectName = null)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("A project path is required.", nameof(projectPath));
        }

        ProjectPath = Path.GetFullPath(projectPath.Trim());
        ProjectName = ResolveProjectName(projectName);
        IsDirty = false;
    }

    public void BeginOperation(
        WorkspaceOperationKind kind,
        string message,
        bool canCancel = true)
    {
        if (Operation.IsBusy)
        {
            throw new InvalidOperationException("Another workspace operation is already running.");
        }

        if (kind == WorkspaceOperationKind.None)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "A running operation must have a concrete kind.");
        }

        Operation = new WorkspaceOperationState(kind, 0d, message, canCancel);
    }

    public void ReportOperation(double progress, string message)
    {
        if (!Operation.IsBusy)
        {
            throw new InvalidOperationException("No workspace operation is running.");
        }

        Operation = new WorkspaceOperationState(
            Operation.Kind,
            progress,
            message,
            Operation.CanCancel);
    }

    public void CompleteOperation(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A completion message is required.", nameof(message));
        }

        Operation = new WorkspaceOperationState(
            WorkspaceOperationKind.None,
            1d,
            message,
            false);
    }

    public void ResetOperation()
    {
        Operation = WorkspaceOperationState.Idle;
    }

    public void AddValidation(WorkspaceValidationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _validationMessages.RemoveAll(item => string.Equals(item.Code, message.Code, StringComparison.OrdinalIgnoreCase));
        _validationMessages.Add(message);
    }

    public void ClearValidation()
    {
        _validationMessages.Clear();
    }

    private void MarkDirty()
    {
        IsDirty = true;
    }

    private void AdvanceDetailRegionRevision()
    {
        DetailRegionRevision = checked(DetailRegionRevision + 1L);
    }

    private void AdvanceSemanticCorrectionRevision()
    {
        SemanticCorrectionRevision = checked(SemanticCorrectionRevision + 1L);
    }

    private string ResolveProjectName(string? projectName)
    {
        if (!string.IsNullOrWhiteSpace(projectName))
        {
            return projectName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(ProjectName))
        {
            return ProjectName;
        }

        if (!string.IsNullOrWhiteSpace(SourcePath))
        {
            return Path.GetFileNameWithoutExtension(SourcePath) ?? "FlowPainter project";
        }

        return "FlowPainter project";
    }

    private static string? NormalizeOptionalPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFullPath(path.Trim());
    }
}
