using System.Text.Json;
using System.Text.Json.Nodes;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Application.Projects;

namespace FlowPainter.Application.Tests.Projects;

public sealed class BoundaryPaintingPersistenceTests
{
    [Fact]
    public async Task ProjectRoundTripPreservesBoundaryPaintingSettings()
    {
        FlowPainterProject project = new(
            "Boundary project",
            "source.png",
            42UL,
            new FlowPainterSettings(
                strokeCount: 4,
                boundaryPainting: CreateCustomSettings()));
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(project, stream);
        stream.Position = 0;
        FlowPainterProject restored = await FlowPainterProjectSerializer.DeserializeAsync(stream);

        AssertBoundarySettings(restored.Settings.BoundaryPainting);
    }

    [Fact]
    public async Task PresetRoundTripPreservesBoundaryPaintingSettings()
    {
        FlowPainterPreset preset = new(
            "Boundary preset",
            new FlowPainterSettings(
                strokeCount: 4,
                boundaryPainting: CreateCustomSettings()));
        await using MemoryStream stream = new();

        await FlowPainterPresetSerializer.SerializeAsync(preset, stream);
        stream.Position = 0;
        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(stream);

        AssertBoundarySettings(restored.Settings.BoundaryPainting);
    }

    [Fact]
    public async Task SchemaSevenProjectWithoutBoundaryPaintingUsesDisabledDefaults()
    {
        FlowPainterProject project = new(
            "Legacy boundary project",
            "source.png",
            11UL,
            new FlowPainterSettings(strokeCount: 2));
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        JsonNode root = JsonNode.Parse(current.ToArray())!;
        root["schemaVersion"] = 7;
        JsonObject settings = root["project"]!["settings"]!.AsObject();
        settings.Remove("boundaryPainting");
        await using MemoryStream legacy = new(JsonSerializer.SerializeToUtf8Bytes(root));

        FlowPainterProject restored = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.False(restored.Settings.BoundaryPainting.Enabled);
        Assert.Equal(
            BoundaryPaintingSettings.DefaultTangentAlignment,
            restored.Settings.BoundaryPainting.TangentAlignment);
    }

    [Fact]
    public async Task SchemaFivePresetWithoutBoundaryPaintingUsesDisabledDefaults()
    {
        FlowPainterPreset preset = new(
            "Legacy boundary preset",
            new FlowPainterSettings(strokeCount: 2));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        JsonNode root = JsonNode.Parse(current.ToArray())!;
        root["schemaVersion"] = 5;
        JsonObject settings = root["preset"]!["settings"]!.AsObject();
        settings.Remove("boundaryPainting");
        await using MemoryStream legacy = new(JsonSerializer.SerializeToUtf8Bytes(root));

        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        Assert.False(restored.Settings.BoundaryPainting.Enabled);
    }

    private static BoundaryPaintingSettings CreateCustomSettings()
    {
        return new BoundaryPaintingSettings(
            enabled: true,
            tangentAlignment: 0.91d,
            alignmentRadius: 7,
            crossingPenalty: 0.88d,
            hardBoundaryThreshold: 0.57d,
            terminationStrength: 0.73d,
            internalEdgeInfluence: 0.36d,
            textureEdgeInfluence: 0.12d,
            contourReinforcement: 0.84d,
            cornerPreservation: 0.79d);
    }

    private static void AssertBoundarySettings(BoundaryPaintingSettings actual)
    {
        Assert.True(actual.Enabled);
        Assert.Equal(0.91d, actual.TangentAlignment);
        Assert.Equal(7, actual.AlignmentRadius);
        Assert.Equal(0.88d, actual.CrossingPenalty);
        Assert.Equal(0.57d, actual.HardBoundaryThreshold);
        Assert.Equal(0.73d, actual.TerminationStrength);
        Assert.Equal(0.36d, actual.InternalEdgeInfluence);
        Assert.Equal(0.12d, actual.TextureEdgeInfluence);
        Assert.Equal(0.84d, actual.ContourReinforcement);
        Assert.Equal(0.79d, actual.CornerPreservation);
    }
}
