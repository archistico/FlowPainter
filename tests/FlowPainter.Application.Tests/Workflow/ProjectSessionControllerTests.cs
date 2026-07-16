using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Workflow;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class ProjectSessionControllerTests
{
    [Fact]
    public async Task CleanSessionAllowsDestructiveActionWithoutPromptingOrSaving()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        ProjectSessionController controller = new(workspace);
        bool prompted = false;
        bool saved = false;

        bool allowed = await controller.ConfirmDestructiveActionAsync(
            _ =>
            {
                prompted = true;
                return Task.FromResult(UnsavedChangesDecision.Cancel);
            },
            _ =>
            {
                saved = true;
                return Task.FromResult(false);
            });

        Assert.True(allowed);
        Assert.False(prompted);
        Assert.False(saved);
    }

    [Fact]
    public async Task CancelKeepsDirtySessionActive()
    {
        FlowPainterWorkspace workspace = CreateDirtyWorkspace();
        ProjectSessionController controller = new(workspace);
        bool saved = false;

        bool allowed = await controller.ConfirmDestructiveActionAsync(
            _ => Task.FromResult(UnsavedChangesDecision.Cancel),
            _ =>
            {
                saved = true;
                return Task.FromResult(true);
            });

        Assert.False(allowed);
        Assert.False(saved);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public async Task DiscardAllowsDestructiveActionWithoutSaving()
    {
        FlowPainterWorkspace workspace = CreateDirtyWorkspace();
        ProjectSessionController controller = new(workspace);
        bool saved = false;

        bool allowed = await controller.ConfirmDestructiveActionAsync(
            _ => Task.FromResult(UnsavedChangesDecision.Discard),
            _ =>
            {
                saved = true;
                return Task.FromResult(false);
            });

        Assert.True(allowed);
        Assert.False(saved);
        Assert.True(workspace.IsDirty);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveAllowsActionOnlyWhenSaveCompletes(bool saveSucceeded)
    {
        FlowPainterWorkspace workspace = CreateDirtyWorkspace();
        ProjectSessionController controller = new(workspace);
        int saveCount = 0;

        bool allowed = await controller.ConfirmDestructiveActionAsync(
            _ => Task.FromResult(UnsavedChangesDecision.Save),
            _ =>
            {
                saveCount++;
                return Task.FromResult(saveSucceeded);
            });

        Assert.Equal(saveSucceeded, allowed);
        Assert.Equal(1, saveCount);
    }

    [Fact]
    public async Task UnknownDecisionIsRejected()
    {
        ProjectSessionController controller = new(CreateDirtyWorkspace());

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ConfirmDestructiveActionAsync(
            _ => Task.FromResult((UnsavedChangesDecision)99),
            _ => Task.FromResult(true)));
    }

    [Fact]
    public void PresentationEditMarksOnlyAnActiveProjectDirty()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        ProjectSessionController controller = new(workspace);

        controller.NotifyProjectEdited();
        Assert.False(workspace.IsDirty);

        workspace.SetSource("source.png");
        workspace.MarkSaved(Path.Combine(Path.GetTempPath(), "project.flowpainter.json"));
        Assert.False(workspace.IsDirty);

        controller.NotifyProjectEdited();
        Assert.True(workspace.IsDirty);
    }

    private static FlowPainterWorkspace CreateWorkspace()
    {
        return new FlowPainterWorkspace(10UL, new FlowPainterSettings());
    }

    private static FlowPainterWorkspace CreateDirtyWorkspace()
    {
        FlowPainterWorkspace workspace = CreateWorkspace();
        workspace.SetSource("source.png");
        return workspace;
    }
}
