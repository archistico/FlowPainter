namespace FlowPainter.Application.FlowPainting.Presets;

public sealed class FlowPainterPresetDocument
{
    public const int MinimumSupportedSchemaVersion = 1;
    public const int CurrentSchemaVersion = 8;

    public FlowPainterPresetDocument(int schemaVersion, FlowPainterPreset preset)
    {
        if (schemaVersion < MinimumSupportedSchemaVersion
            || schemaVersion > CurrentSchemaVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                $"Supported preset schema versions are {MinimumSupportedSchemaVersion} through {CurrentSchemaVersion}.");
        }

        ArgumentNullException.ThrowIfNull(preset);
        SchemaVersion = schemaVersion;
        Preset = preset;
    }

    public int SchemaVersion { get; }

    public FlowPainterPreset Preset { get; }
}
