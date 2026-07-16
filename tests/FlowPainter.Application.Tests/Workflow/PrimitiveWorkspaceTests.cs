using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Workflow;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class PrimitiveWorkspaceTests
{
    [Fact]
    public void SetGenerativeModeMarksWorkspaceDirtyOnlyWhenChanged()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.LoadProject(CreateProject());

        workspace.SetGenerativeMode(GenerativeMode.FlowPainting);
        Assert.False(workspace.IsDirty);

        workspace.SetGenerativeMode(GenerativeMode.GeometricPrimitives);
        Assert.True(workspace.IsDirty);
        Assert.Equal(GenerativeMode.GeometricPrimitives, workspace.Mode);
    }

    [Fact]
    public void SetPrimitiveGenerationIsCapturedByProject()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.SetSource("source.png");
        PrimitiveGenerationSettings settings = new(
            primitiveCount: 123,
            allowedKinds: PrimitiveKindSet.Circle);

        workspace.SetGenerativeMode(GenerativeMode.GeometricPrimitives);
        workspace.SetPrimitiveGeneration(settings);
        FlowPainterProject project = workspace.CreateProject("Primitive");

        Assert.Equal(GenerativeMode.GeometricPrimitives, project.Mode);
        Assert.Equal(123, project.PrimitiveGeneration.PrimitiveCount);
        Assert.Equal(PrimitiveKindSet.Circle, project.PrimitiveGeneration.AllowedKinds);
    }

    [Fact]
    public void LoadProjectRestoresPrimitiveState()
    {
        FlowPainterProject project = new(
            "Primitive",
            "source.png",
            2UL,
            new FlowPainterSettings(),
            mode: GenerativeMode.GeometricPrimitives,
            primitiveGeneration: new PrimitiveGenerationSettings(primitiveCount: 88));
        FlowPainterWorkspace workspace = CreateWorkspace();

        workspace.LoadProject(project);

        Assert.False(workspace.IsDirty);
        Assert.Equal(GenerativeMode.GeometricPrimitives, workspace.Mode);
        Assert.Equal(88, workspace.PrimitiveGeneration.PrimitiveCount);
    }

    private static FlowPainterWorkspace CreateWorkspace()
    {
        return new FlowPainterWorkspace(1UL, new FlowPainterSettings());
    }

    private static FlowPainterProject CreateProject()
    {
        return new FlowPainterProject("Flow", "source.png", 1UL, new FlowPainterSettings());
    }
}
