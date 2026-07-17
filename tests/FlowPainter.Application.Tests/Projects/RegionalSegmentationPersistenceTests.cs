using System.Text;
using System.Text.Json.Nodes;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Segmentation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Projects;

public sealed class RegionalSegmentationPersistenceTests
{
    [Fact]
    public async Task ProjectRoundTripPreservesRegionalSettingsAndRoleOverrides()
    {
        RegionSegmentationSettings segmentation = new(48, 7.5d, 1.2d, 17, 0.25d, enabled: false);
        RegionMergeSettings merge = RegionMergeIntensityMapper.Create(72d);
        RegionRoleOverride roleOverride = new(
            "region-role-1",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.7d),
            RegionRole.Focal,
            "Face",
            "regional-42");
        FlowPainterProject project = new(
            "Regional",
            "images/source.png",
            17UL,
            new FlowPainterSettings(regionalSegmentation: segmentation, regionMerge: merge),
            regionRoleOverrides: new[] { roleOverride });
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(project, stream);
        stream.Position = 0L;
        JsonObject projectDocument = (await JsonNode.ParseAsync(stream))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project document is empty.");
        Assert.Equal(12, projectDocument["schemaVersion"]?.GetValue<int>());
        stream.Position = 0L;
        FlowPainterProject restored = await FlowPainterProjectSerializer.DeserializeAsync(stream);

        AssertSegmentationEqual(segmentation, restored.Settings.RegionalSegmentation);
        AssertMergeEqual(merge, restored.Settings.RegionMerge);
        RegionRoleOverride restoredOverride = Assert.Single(restored.RegionRoleOverrides);
        Assert.Equal(roleOverride, restoredOverride);
    }

    [Fact]
    public async Task ProjectSchemaElevenDefaultsRegionalSettings()
    {
        SemanticCorrectionRegion correction = new(
            "legacy-role",
            new NormalizedRect(0.2d, 0.2d, 0.6d, 0.6d),
            SemanticCorrectionKind.ForceBackground,
            "Legacy background");
        FlowPainterProject project = new(
            "Legacy regional",
            "images/source.png",
            18UL,
            new FlowPainterSettings(
                regionalSegmentation: new RegionSegmentationSettings(32, 4d),
                regionMerge: RegionMergeIntensityMapper.Create(85d)),
            semanticCorrections: new[] { correction });
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 11;
        JsonObject projectNode = root["project"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no project payload.");
        JsonObject settings = projectNode["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no settings.");
        projectNode.Remove("regionRoleOverrides");
        settings.Remove("regionalSegmentation");
        settings.Remove("regionMerge");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject restored = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        AssertSegmentationEqual(new RegionSegmentationSettings(), restored.Settings.RegionalSegmentation);
        AssertMergeEqual(new RegionMergeSettings(), restored.Settings.RegionMerge);
        RegionRoleOverride migratedRole = Assert.Single(restored.RegionRoleOverrides);
        Assert.Equal(RegionRole.Background, migratedRole.Role);
        Assert.Equal(correction.Id, migratedRole.Id);
    }

    [Fact]
    public async Task PresetRoundTripPreservesRegionalSettings()
    {
        RegionSegmentationSettings segmentation = new(96, 13d, 0.4d, 21, 0.75d, enabled: true);
        RegionMergeSettings merge = RegionMergeIntensityMapper.Create(33d);
        FlowPainterPreset preset = new(
            "Regional preset",
            new FlowPainterSettings(regionalSegmentation: segmentation, regionMerge: merge));
        await using MemoryStream stream = new();

        await FlowPainterPresetSerializer.SerializeAsync(preset, stream);
        stream.Position = 0L;
        JsonObject presetDocument = (await JsonNode.ParseAsync(stream))?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset document is empty.");
        Assert.Equal(9, presetDocument["schemaVersion"]?.GetValue<int>());
        stream.Position = 0L;
        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(stream);

        AssertSegmentationEqual(segmentation, restored.Settings.RegionalSegmentation);
        AssertMergeEqual(merge, restored.Settings.RegionMerge);
    }

    [Fact]
    public async Task PresetSchemaEightDefaultsRegionalSettings()
    {
        FlowPainterPreset preset = new(
            "Legacy regional preset",
            new FlowPainterSettings(
                regionalSegmentation: new RegionSegmentationSettings(24, 3d),
                regionMerge: RegionMergeIntensityMapper.Create(90d)));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON is empty.");
        root["schemaVersion"] = 8;
        JsonObject settings = root["preset"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON has no settings.");
        settings.Remove("regionalSegmentation");
        settings.Remove("regionMerge");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        AssertSegmentationEqual(new RegionSegmentationSettings(), restored.Settings.RegionalSegmentation);
        AssertMergeEqual(new RegionMergeSettings(), restored.Settings.RegionMerge);
    }

    private static void AssertSegmentationEqual(
        RegionSegmentationSettings expected,
        RegionSegmentationSettings actual)
    {
        Assert.Equal(expected.Enabled, actual.Enabled);
        Assert.Equal(expected.TargetRegionSize, actual.TargetRegionSize);
        Assert.Equal(expected.Compactness, actual.Compactness);
        Assert.Equal(expected.PreBlurSigma, actual.PreBlurSigma);
        Assert.Equal(expected.MaximumIterations, actual.MaximumIterations);
        Assert.Equal(expected.ConvergenceTolerance, actual.ConvergenceTolerance);
    }

    private static void AssertMergeEqual(RegionMergeSettings expected, RegionMergeSettings actual)
    {
        Assert.Equal(expected.IntermediateTargetRatio, actual.IntermediateTargetRatio);
        Assert.Equal(expected.BroadMassTargetRatio, actual.BroadMassTargetRatio);
        Assert.Equal(expected.IntermediateMaximumCost, actual.IntermediateMaximumCost);
        Assert.Equal(expected.BroadMassMaximumCost, actual.BroadMassMaximumCost);
        Assert.Equal(expected.StrongBoundaryThreshold, actual.StrongBoundaryThreshold);
        Assert.Equal(expected.MaximumParentAreaFraction, actual.MaximumParentAreaFraction);
    }
}
