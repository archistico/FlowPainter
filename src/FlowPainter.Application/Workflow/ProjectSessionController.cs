namespace FlowPainter.Application.Workflow;

public sealed class ProjectSessionController
{
    private readonly FlowPainterWorkspace _workspace;

    public ProjectSessionController(FlowPainterWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _workspace = workspace;
    }

    public bool HasUnsavedChanges => _workspace.HasSource && _workspace.IsDirty;

    public void NotifyProjectEdited()
    {
        _workspace.MarkProjectEdited();
    }

    public async Task<bool> ConfirmDestructiveActionAsync(
        Func<CancellationToken, Task<UnsavedChangesDecision>> requestDecisionAsync,
        Func<CancellationToken, Task<bool>> saveAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDecisionAsync);
        ArgumentNullException.ThrowIfNull(saveAsync);

        if (!HasUnsavedChanges)
        {
            return true;
        }

        UnsavedChangesDecision decision = await requestDecisionAsync(cancellationToken).ConfigureAwait(true);
        return decision switch
        {
            UnsavedChangesDecision.Discard => true,
            UnsavedChangesDecision.Save => await saveAsync(cancellationToken).ConfigureAwait(true),
            UnsavedChangesDecision.Cancel => false,
            _ => throw new InvalidOperationException($"Unsupported unsaved-changes decision: {decision}.")
        };
    }
}
