using System.Text.Json;
using System.Text.Json.Nodes;
using FlowPainter.Application.Background;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Application.Projects;

namespace FlowPainter.Application.Tests.Projects;

public sealed class BackgroundSuppressionPersistenceTests
{
    [Fact]
    public async Task ProjectRoundTripPreservesBackgroundSuppressionSettings()
    {
        FlowPainterProject project = new(
            "Background project",
            "source.png",
            42UL,
            new FlowPainterSettings(strokeCount: 4, backgroundSuppression: CreateCustomSettings()));
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(project, stream);
        stream.Position = 0;
        FlowPainterProject restored = await FlowPainterProjectSerializer.DeserializeAsync(stream);

        AssertSettings(restored.Settings.BackgroundSuppression);
    }

    [Fact]
    public async Task PresetRoundTripPreservesBackgroundSuppressionSettings()
    {
        FlowPainterPreset preset = new(
            "Background preset",
            new FlowPainterSettings(strokeCount: 4, backgroundSuppression: CreateCustomSettings()));
        await using MemoryStream stream = new();

        await FlowPainterPresetSerializer.SerializeAsync(preset, stream);
        stream.Position = 0;
        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(stream);

        AssertSettings(restored.Settings.BackgroundSuppression);
    }

    [Fact]
    public async Task SchemaEightProjectWithoutSuppressionUsesDisabledDefaults()
    {
        FlowPainterProject project = new(
            "Legacy project",
            "source.png",
            11UL,
            new FlowPainterSettings(strokeCount: 2, backgroundSuppression: CreateCustomSettings()));
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        JsonNode root = JsonNode.Parse(current.ToArray())!;
        root["schemaVersion"] = 8;
        root["project"]!["settings"]!.AsObject().Remove("backgroundSuppression");
        await using MemoryStream legacy = new(JsonSerializer.SerializeToUtf8Bytes(root));

        FlowPainterProject restored = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.False(restored.Settings.BackgroundSuppression.Enabled);
    }

    [Fact]
    public async Task SchemaSixPresetWithoutSuppressionUsesDisabledDefaults()
    {
        FlowPainterPreset preset = new(
            "Legacy preset",
            new FlowPainterSettings(strokeCount: 2, backgroundSuppression: CreateCustomSettings()));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        JsonNode root = JsonNode.Parse(current.ToArray())!;
        root["schemaVersion"] = 6;
        root["preset"]!["settings"]!.AsObject().Remove("backgroundSuppression");
        await using MemoryStream legacy = new(JsonSerializer.SerializeToUtf8Bytes(root));

        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        Assert.False(restored.Settings.BackgroundSuppression.Enabled);
    }

    private static BackgroundSuppressionSettings CreateCustomSettings()
    {
        return new BackgroundSuppressionSettings(
            enabled: true,
            overallStrength: 0.91d,
            detailFloor: 0.13d,
            uncertaintyProtection: 0.82d,
            silhouetteProtection: 0.97d,
            transitionSoftness: 0.63d,
            backgroundPlacementWeight: 0.28d,
            strokeLengthMultiplier: 2.2d,
            strokeWidthMultiplier: 1.8d,
            segmentMultiplier: 0.54d,
            curveFreedomMultiplier: 1.72d,
            colorSimplification: 0.36d);
    }

    private static void AssertSettings(BackgroundSuppressionSettings actual)
    {
        Assert.True(actual.Enabled);
        Assert.Equal(0.91d, actual.OverallStrength);
        Assert.Equal(0.13d, actual.DetailFloor);
        Assert.Equal(0.82d, actual.UncertaintyProtection);
        Assert.Equal(0.97d, actual.SilhouetteProtection);
        Assert.Equal(0.63d, actual.TransitionSoftness);
        Assert.Equal(0.28d, actual.BackgroundPlacementWeight);
        Assert.Equal(2.2d, actual.StrokeLengthMultiplier);
        Assert.Equal(1.8d, actual.StrokeWidthMultiplier);
        Assert.Equal(0.54d, actual.SegmentMultiplier);
        Assert.Equal(1.72d, actual.CurveFreedomMultiplier);
        Assert.Equal(0.36d, actual.ColorSimplification);
    }
}
