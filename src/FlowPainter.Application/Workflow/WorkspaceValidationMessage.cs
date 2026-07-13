namespace FlowPainter.Application.Workflow;

public sealed record WorkspaceValidationMessage
{
    public WorkspaceValidationMessage(
        string code,
        string message,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A validation message must have a code.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A validation message must have text.", nameof(message));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown validation severity.");
        }

        Code = code.Trim();
        Message = message.Trim();
        Severity = severity;
    }

    public string Code { get; }

    public string Message { get; }

    public ValidationSeverity Severity { get; }
}
