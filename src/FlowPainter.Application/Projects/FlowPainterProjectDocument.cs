namespace FlowPainter.Application.Projects;

public sealed class FlowPainterProjectDocument
{
    public const int CurrentSchemaVersion = 1;

    public FlowPainterProjectDocument(int schemaVersion, FlowPainterProject project)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                $"Only project schema version {CurrentSchemaVersion} is supported.");
        }

        ArgumentNullException.ThrowIfNull(project);
        SchemaVersion = schemaVersion;
        Project = project;
    }

    public int SchemaVersion { get; }

    public FlowPainterProject Project { get; }
}
