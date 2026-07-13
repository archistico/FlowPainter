using FlowPainter.Application.Workflow;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class WorkspaceOperationStateTests
{
    [Fact]
    public void IdleIsNotBusyOrCancellable()
    {
        Assert.False(WorkspaceOperationState.Idle.IsBusy);
        Assert.False(WorkspaceOperationState.Idle.CanCancel);
        Assert.Equal(WorkspaceOperationKind.None, WorkspaceOperationState.Idle.Kind);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidProgress(double progress)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkspaceOperationState(
            WorkspaceOperationKind.RenderingPreview,
            progress,
            "Rendering",
            true));
    }

    [Fact]
    public void ConstructorRejectsCancellableIdleState()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceOperationState(
            WorkspaceOperationKind.None,
            0d,
            "Idle",
            true));
    }

    [Fact]
    public void ConstructorTrimsMessage()
    {
        WorkspaceOperationState state = new(
            WorkspaceOperationKind.AnalyzingDetail,
            0.25d,
            "  Analyzing  ",
            true);

        Assert.Equal("Analyzing", state.Message);
        Assert.True(state.IsBusy);
    }
}
