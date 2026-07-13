namespace FlowPainter.Application.Projects;

public sealed class FlowPainterProjectDocument
{
    public const int CurrentSchemaVersion = 4;
    public const int MinimumSupportedSchemaVersion = 1;

    public FlowPainterProjectDocument(int schemaVersion, FlowPainterProject project)
    {
        if (schemaVersion < MinimumSupportedSchemaVersion || schemaVersion > CurrentSchemaVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                $"Project schema versions {MinimumSupportedSchemaVersion} through {CurrentSchemaVersion} are supported.");
        }

        ArgumentNullException.ThrowIfNull(project);
        SchemaVersion = schemaVersion;
        Project = project;
    }

    public int SchemaVersion { get; }

    public FlowPainterProject Project { get; }
}
