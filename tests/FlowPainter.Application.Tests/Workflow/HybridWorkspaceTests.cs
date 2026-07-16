using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Workflow;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Hybrid;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class HybridWorkspaceTests
{
    [Fact]
    public void SetHybridGenerationMarksWorkspaceDirty()
    {
        FlowPainterWorkspace workspace = new(1UL, new FlowPainterSettings());
        HybridGenerationSettings settings = new(influenceKind: PrimitiveFlowInfluenceKind.Vortex);

        workspace.SetHybridGeneration(settings);

        Assert.Same(settings, workspace.HybridGeneration);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void ProjectRoundTripThroughWorkspacePreservesHybridState()
    {
        HybridGenerationSettings settings = new(
            primitiveBudgetFraction: 0.30d,
            flowBudgetFraction: 0.50d,
            refinementBudgetFraction: 0.20d,
            influenceKind: PrimitiveFlowInfluenceKind.AxisAlignment);
        FlowPainterWorkspace workspace = new(
            8UL,
            new FlowPainterSettings(),
            mode: GenerativeMode.Hybrid,
            hybridGeneration: settings);
        workspace.SetSource("source.png");

        FlowPainterProject project = workspace.CreateProject("Hybrid");
        FlowPainterWorkspace restored = new(1UL, new FlowPainterSettings());
        restored.LoadProject(project);

        Assert.Equal(GenerativeMode.Hybrid, restored.Mode);
        Assert.Equal(PrimitiveFlowInfluenceKind.AxisAlignment, restored.HybridGeneration.InfluenceKind);
        Assert.Equal(0.30d, restored.HybridGeneration.PrimitiveBudgetFraction, 12);
        Assert.False(restored.IsDirty);
    }
}
