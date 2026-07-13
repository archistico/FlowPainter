namespace FlowPainter.Application.Workflow;

public sealed class RecentItemsDocument
{
    public const int CurrentSchemaVersion = 1;

    private readonly IReadOnlyList<string> _projects;
    private readonly IReadOnlyList<string> _presets;

    public RecentItemsDocument(
        int schemaVersion,
        IReadOnlyList<string>? projects = null,
        IReadOnlyList<string>? presets = null)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                $"Only recent-items schema version {CurrentSchemaVersion} is supported.");
        }

        SchemaVersion = schemaVersion;
        _projects = Array.AsReadOnly(projects?.ToArray() ?? []);
        _presets = Array.AsReadOnly(presets?.ToArray() ?? []);
    }

    public int SchemaVersion { get; }

    public IReadOnlyList<string> Projects => _projects;

    public IReadOnlyList<string> Presets => _presets;
}
