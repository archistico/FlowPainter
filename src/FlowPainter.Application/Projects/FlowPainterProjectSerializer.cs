using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Projects;

public static class FlowPainterProjectSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public static async Task SerializeAsync(
        FlowPainterProject project,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
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

        FlowPainterProjectDocument document = new(
            FlowPainterProjectDocument.CurrentSchemaVersion,
            project);
        await JsonSerializer.SerializeAsync(
            destination,
            document,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<FlowPainterProject> DeserializeAsync(
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
            throw new InvalidDataException("The project schema version is missing or invalid.");
        }

        if (schemaVersion < FlowPainterProjectDocument.MinimumSupportedSchemaVersion
            || schemaVersion > FlowPainterProjectDocument.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Project schema version {schemaVersion} is not supported. Supported versions: "
                + $"{FlowPainterProjectDocument.MinimumSupportedSchemaVersion}-{FlowPainterProjectDocument.CurrentSchemaVersion}.");
        }

        if (!root.TryGetProperty("project", out JsonElement projectElement))
        {
            throw new InvalidDataException("The project payload is missing.");
        }

        JsonNode projectNode = JsonNode.Parse(projectElement.GetRawText())
            ?? throw new InvalidDataException("The project payload is empty or invalid.");
        ApplyCompatibilityDefaults(projectNode, schemaVersion);
        FlowPainterProject? project = JsonSerializer.Deserialize<FlowPainterProject>(
            projectNode.ToJsonString(),
            SerializerOptions);
        return project
            ?? throw new InvalidDataException("The project document is empty or invalid.");
    }

    private static void ApplyCompatibilityDefaults(JsonNode projectNode, int schemaVersion)
    {
        if (projectNode is not JsonObject project)
        {
            return;
        }

        if (schemaVersion < 10
            && project["settings"] is JsonObject settings
            && settings["detailInfluence"] is JsonObject detailInfluence
            && !detailInfluence.ContainsKey("regionTransitionWidth"))
        {
            detailInfluence["regionTransitionWidth"] =
                DetailInfluenceSettings.DefaultRegionTransitionWidth;
        }

        if (schemaVersion < 11 && !project.ContainsKey("semanticCorrections"))
        {
            project["semanticCorrections"] = new JsonArray();
        }

        if (schemaVersion < 12
            && project["settings"] is JsonObject regionalSettings)
        {
            if (!regionalSettings.ContainsKey("regionalSegmentation"))
            {
                regionalSettings["regionalSegmentation"] = JsonSerializer.SerializeToNode(
                    new RegionSegmentationSettings(),
                    SerializerOptions);
            }

            if (!regionalSettings.ContainsKey("regionMerge"))
            {
                regionalSettings["regionMerge"] = JsonSerializer.SerializeToNode(
                    new RegionMergeSettings(),
                    SerializerOptions);
            }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new NormalizedRectJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
