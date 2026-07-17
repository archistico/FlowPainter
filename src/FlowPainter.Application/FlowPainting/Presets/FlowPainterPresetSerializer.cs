using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.FlowPainting.Presets;

public static class FlowPainterPresetSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public static async Task SerializeAsync(
        FlowPainterPreset preset,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (destination.CanSeek)
        {
            destination.Position = 0L;
            destination.SetLength(0L);
        }

        FlowPainterPresetDocument document = new(
            FlowPainterPresetDocument.CurrentSchemaVersion,
            preset);
        await JsonSerializer.SerializeAsync(
            destination,
            document,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<FlowPainterPreset> DeserializeAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }

        using JsonDocument jsonDocument = await JsonDocument.ParseAsync(
            source,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        JsonElement root = jsonDocument.RootElement;
        if (!root.TryGetProperty("schemaVersion", out JsonElement schemaElement)
            || !schemaElement.TryGetInt32(out int schemaVersion))
        {
            throw new InvalidDataException("The preset schema version is missing or invalid.");
        }

        if (schemaVersion < FlowPainterPresetDocument.MinimumSupportedSchemaVersion
            || schemaVersion > FlowPainterPresetDocument.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Preset schema version {schemaVersion} is not supported. Supported versions are {FlowPainterPresetDocument.MinimumSupportedSchemaVersion} through {FlowPainterPresetDocument.CurrentSchemaVersion}.");
        }

        JsonNode documentNode = JsonNode.Parse(root.GetRawText())
            ?? throw new InvalidDataException("The preset document is empty or invalid.");
        ApplyCompatibilityDefaults(documentNode, schemaVersion);
        FlowPainterPresetDocument? document = JsonSerializer.Deserialize<FlowPainterPresetDocument>(
            documentNode.ToJsonString(),
            SerializerOptions);
        return document?.Preset
            ?? throw new InvalidDataException("The preset document is empty or invalid.");
    }

    private static void ApplyCompatibilityDefaults(JsonNode documentNode, int schemaVersion)
    {
        if (documentNode is not JsonObject document
            || document["preset"] is not JsonObject preset
            || preset["settings"] is not JsonObject settings)
        {
            return;
        }

        if (schemaVersion < 8
            && settings["detailInfluence"] is JsonObject detailInfluence
            && !detailInfluence.ContainsKey("regionTransitionWidth"))
        {
            detailInfluence["regionTransitionWidth"] =
                DetailInfluenceSettings.DefaultRegionTransitionWidth;
        }

        if (schemaVersion < 10
            && settings["detailInfluence"] is JsonObject strokePolicy)
        {
            ApplyHighDetailStrokePolicyDefaults(strokePolicy);
        }

        if (schemaVersion < 9)
        {
            if (!settings.ContainsKey("regionalSegmentation"))
            {
                settings["regionalSegmentation"] = JsonSerializer.SerializeToNode(
                    new RegionSegmentationSettings(),
                    SerializerOptions);
            }

            if (!settings.ContainsKey("regionMerge"))
            {
                settings["regionMerge"] = JsonSerializer.SerializeToNode(
                    new RegionMergeSettings(),
                    SerializerOptions);
            }
        }
    }


    private static void ApplyHighDetailStrokePolicyDefaults(JsonObject detailInfluence)
    {
        detailInfluence["detailedSegmentMultiplier"] =
            DetailInfluenceSettings.DefaultDetailedSegmentMultiplier;
        detailInfluence["backgroundSegmentMultiplier"] =
            DetailInfluenceSettings.DefaultBackgroundSegmentMultiplier;
        detailInfluence["detailedCurveMultiplier"] =
            DetailInfluenceSettings.DefaultDetailedCurveMultiplier;
        detailInfluence["backgroundCurveMultiplier"] =
            DetailInfluenceSettings.DefaultBackgroundCurveMultiplier;
        detailInfluence["detailedTangentAlignmentBoost"] =
            DetailInfluenceSettings.DefaultDetailedTangentAlignmentBoost;
        detailInfluence["detailedCrossingResistanceBoost"] =
            DetailInfluenceSettings.DefaultDetailedCrossingResistanceBoost;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
