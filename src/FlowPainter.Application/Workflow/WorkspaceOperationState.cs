namespace FlowPainter.Application.Workflow;

public sealed record WorkspaceOperationState
{
    public static WorkspaceOperationState Idle { get; } = new(
        WorkspaceOperationKind.None,
        0d,
        "Ready.",
        false);

    public WorkspaceOperationState(
        WorkspaceOperationKind kind,
        double progress,
        string message,
        bool canCancel)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown operation kind.");
        }

        if (!double.IsFinite(progress) || progress < 0d || progress > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(progress),
                progress,
                "Operation progress must be between 0 and 1.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("An operation state must have a message.", nameof(message));
        }

        if (kind == WorkspaceOperationKind.None && canCancel)
        {
            throw new ArgumentException("An idle operation cannot be cancellable.", nameof(canCancel));
        }

        Kind = kind;
        Progress = progress;
        Message = message.Trim();
        CanCancel = canCancel;
    }

    public WorkspaceOperationKind Kind { get; }

    public double Progress { get; }

    public string Message { get; }

    public bool CanCancel { get; }

    public bool IsBusy => Kind != WorkspaceOperationKind.None;
}
