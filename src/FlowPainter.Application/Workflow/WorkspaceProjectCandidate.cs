using FlowPainter.Application.Projects;

namespace FlowPainter.Application.Workflow;

public sealed class WorkspaceProjectCandidate
{
    internal WorkspaceProjectCandidate(FlowPainterProject project, string? projectPath)
    {
        Project = project;
        ProjectPath = projectPath;
    }

    public FlowPainterProject Project { get; }

    public string? ProjectPath { get; }
}
