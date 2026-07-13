using FlowPainter.Application.Workflow;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class WorkspaceValidationMessageTests
{
    [Fact]
    public void ConstructorTrimsCodeAndMessage()
    {
        WorkspaceValidationMessage message = new("  source.missing  ", "  Source is missing.  ");

        Assert.Equal("source.missing", message.Code);
        Assert.Equal("Source is missing.", message.Message);
    }

    [Theory]
    [InlineData("", "Message")]
    [InlineData("Code", "")]
    public void ConstructorRejectsEmptyValues(string code, string message)
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceValidationMessage(code, message));
    }

    [Fact]
    public void ConstructorRejectsUnknownSeverity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkspaceValidationMessage(
            "code",
            "message",
            (ValidationSeverity)99));
    }
}
