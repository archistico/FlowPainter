using System.Text;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Presets;

public sealed class FlowPainterPresetSerializerTests
{
    [Fact]
    public async Task RoundTripPreservesEverySetting()
    {
        FlowPainterPreset original = new(
            "Round trip",
            new FlowPainterSettings(
                new FlowFieldSettings(
                    FlowFieldKind.LegacyTrigonometric,
                    7.5d,
                    2,
                    0.7d,
                    1.8d,
                    0.25d),
                321,
                17,
                640,
                13d,
                0.008d,
                0.9d,
                2d,
                8d,
                0.6d,
                StrokePlanBackgroundMode.Transparent,
                new DetailAnalysisSettings(0.2d, 0.8d, 0.3d, 2),
                new DetailInfluenceSettings(6d, 0.4d, 1.6d, 0.5d, 1.7d, 0.09d),
                new BrushSettings(BrushKind.Bristle, 0.45d, 0.2d, 0.3d, 11, 0.85d),
                new SemanticAnalysisSettings(false, 0.4d, 0d, 0d, 0d, 0d, 0.6d, 0.01d, 3, 0.5d, 1, 4),
                new SceneBoundaryAnalysisSettings(
                    luminanceWeight: 0.7d,
                    colorWeight: 0.8d,
                    continuityWeight: 1.1d,
                    textureSuppression: 0.72d,
                    edgeThreshold: 0.12d,
                    importantEdgeThreshold: 0.35d,
                    coarseRadius: 4,
                    smoothingRadius: 2,
                    boundaryProtectionRadius: 6)));
        await using MemoryStream stream = new();

        await FlowPainterPresetSerializer.SerializeAsync(original, stream);
        stream.Position = 0L;
        FlowPainterPreset restored = await FlowPainterPresetSerializer.DeserializeAsync(stream);

        Assert.Equal(original.Name, restored.Name);
        AssertSettingsEqual(original.Settings, restored.Settings);
    }

    [Fact]
    public async Task SerializeTruncatesExistingSeekableContent()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(new string('x', 10_000)), writable: true);
        FlowPainterPreset preset = new("Small", new FlowPainterSettings(strokeCount: 1));

        await FlowPainterPresetSerializer.SerializeAsync(preset, stream);

        Assert.True(stream.Length < 10_000);
        stream.Position = 0L;
        _ = await FlowPainterPresetSerializer.DeserializeAsync(stream);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionSevenWithDefaultRegionTransition()
    {
        FlowPainterPreset preset = new(
            "Legacy soft regions",
            new FlowPainterSettings(
                detailInfluence: new DetailInfluenceSettings(regionTransitionWidth: 0.12d)));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        current.Position = 0L;

        System.Text.Json.Nodes.JsonObject root =
            (await System.Text.Json.Nodes.JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON is empty.");
        root["schemaVersion"] = 7;
        System.Text.Json.Nodes.JsonObject detailInfluence =
            root["preset"]?["settings"]?["detailInfluence"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset has no detail-influence settings.");
        detailInfluence.Remove("regionTransitionWidth");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterPreset loaded = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        Assert.Equal(
            DetailInfluenceSettings.DefaultRegionTransitionWidth,
            loaded.Settings.DetailInfluence.RegionTransitionWidth);
    }

    [Fact]
    public async Task DeserializeRejectsUnsupportedSchemaVersion()
    {
        await using MemoryStream valid = new();
        await FlowPainterPresetSerializer.SerializeAsync(
            new FlowPainterPreset("Invalid version", new FlowPainterSettings(strokeCount: 1)),
            valid);
        string json = Encoding.UTF8.GetString(valid.ToArray())
            .Replace("\"schemaVersion\": 9", "\"schemaVersion\": 99", StringComparison.Ordinal);
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(json), writable: false);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => FlowPainterPresetSerializer.DeserializeAsync(stream));
    }


    [Fact]
    public async Task DeserializeMigratesSchemaVersionOneWithDetailDefaults()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "preset": {
                "name": "M3 preset",
                "settings": {
                  "field": {
                    "kind": "CoherentNoise",
                    "scale": 3.5,
                    "octaves": 4,
                    "persistence": 0.55,
                    "lacunarity": 2,
                    "angleOffsetRadians": 0
                  },
                  "strokeCount": 12000,
                  "segmentCount": 20,
                  "referenceMaximumDimension": 512,
                  "uniformDensity": 18,
                  "lengthScale": 0.005,
                  "maximumCurveRadians": 0.5,
                  "minimumStrokeWidthPixels": 3,
                  "maximumStrokeWidthPixels": 7,
                  "strokeOpacity": 0.85,
                  "backgroundMode": "SourceImage"
                }
              }
            }
            """;
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(json), writable: false);

        FlowPainterPreset preset = await FlowPainterPresetSerializer.DeserializeAsync(stream);

        Assert.Equal("M3 preset", preset.Name);
        Assert.Equal(DetailAnalysisSettings.DefaultBaseDetail, preset.Settings.DetailAnalysis.BaseDetail);
        Assert.Equal(DetailInfluenceSettings.DefaultPlacementBias, preset.Settings.DetailInfluence.PlacementBias);
        Assert.Equal(DetailInfluenceSettings.DefaultRegionTransitionWidth, preset.Settings.DetailInfluence.RegionTransitionWidth);
        Assert.Equal(BrushKind.SolidRound, preset.Settings.Brush.Kind);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionTwoWithDefaultBrush()
    {
        FlowPainterPreset preset = new("M6 preset", new FlowPainterSettings(strokeCount: 1));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        string json = Encoding.UTF8.GetString(current.ToArray())
            .Replace("\"schemaVersion\": 9", "\"schemaVersion\": 2", StringComparison.Ordinal);
        System.Text.Json.Nodes.JsonObject root = System.Text.Json.Nodes.JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON is empty.");
        System.Text.Json.Nodes.JsonObject settings = root["preset"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON has no settings.");
        settings.Remove("brush");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterPreset loaded = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        Assert.Equal(BrushKind.SolidRound, loaded.Settings.Brush.Kind);
        Assert.Equal(BrushSettings.DefaultHardness, loaded.Settings.Brush.Hardness);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionThreeWithSemanticDefaults()
    {
        FlowPainterPreset preset = new("M7 preset", new FlowPainterSettings(strokeCount: 1));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        string json = Encoding.UTF8.GetString(current.ToArray())
            .Replace("\"schemaVersion\": 9", "\"schemaVersion\": 3", StringComparison.Ordinal);
        System.Text.Json.Nodes.JsonObject root = System.Text.Json.Nodes.JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON is empty.");
        System.Text.Json.Nodes.JsonObject settings = root["preset"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON has no settings.");
        settings.Remove("semanticAnalysis");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterPreset loaded = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        Assert.True(loaded.Settings.SemanticAnalysis.Enabled);
        Assert.Equal(
            SemanticAnalysisSettings.DefaultOverallInfluence,
            loaded.Settings.SemanticAnalysis.OverallInfluence);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionFourWithBoundaryDefaults()
    {
        FlowPainterPreset preset = new("M8 preset", new FlowPainterSettings(strokeCount: 1));
        await using MemoryStream current = new();
        await FlowPainterPresetSerializer.SerializeAsync(preset, current);
        string json = Encoding.UTF8.GetString(current.ToArray())
            .Replace("\"schemaVersion\": 9", "\"schemaVersion\": 4", StringComparison.Ordinal);
        System.Text.Json.Nodes.JsonObject root = System.Text.Json.Nodes.JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON is empty.");
        System.Text.Json.Nodes.JsonObject settings = root["preset"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized preset JSON has no settings.");
        settings.Remove("boundaryAnalysis");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterPreset loaded = await FlowPainterPresetSerializer.DeserializeAsync(legacy);

        Assert.True(loaded.Settings.BoundaryAnalysis.Enabled);
        Assert.Equal(
            SceneBoundaryAnalysisSettings.DefaultImportantEdgeThreshold,
            loaded.Settings.BoundaryAnalysis.ImportantEdgeThreshold);
    }

    [Fact]
    public async Task DeserializeHonorsCancellation()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{}"), writable: false);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => FlowPainterPresetSerializer.DeserializeAsync(stream, cancellation.Token));
    }

    private static void AssertSettingsEqual(FlowPainterSettings expected, FlowPainterSettings actual)
    {
        Assert.Equal(expected.StrokeCount, actual.StrokeCount);
        Assert.Equal(expected.SegmentCount, actual.SegmentCount);
        Assert.Equal(expected.ReferenceMaximumDimension, actual.ReferenceMaximumDimension);
        Assert.Equal(expected.UniformDensity, actual.UniformDensity);
        Assert.Equal(expected.LengthScale, actual.LengthScale);
        Assert.Equal(expected.MaximumCurveRadians, actual.MaximumCurveRadians);
        Assert.Equal(expected.MinimumStrokeWidthPixels, actual.MinimumStrokeWidthPixels);
        Assert.Equal(expected.MaximumStrokeWidthPixels, actual.MaximumStrokeWidthPixels);
        Assert.Equal(expected.StrokeOpacity, actual.StrokeOpacity);
        Assert.Equal(expected.BackgroundMode, actual.BackgroundMode);
        Assert.Equal(expected.Field.Kind, actual.Field.Kind);
        Assert.Equal(expected.Field.Scale, actual.Field.Scale);
        Assert.Equal(expected.Field.Octaves, actual.Field.Octaves);
        Assert.Equal(expected.Field.Persistence, actual.Field.Persistence);
        Assert.Equal(expected.Field.Lacunarity, actual.Field.Lacunarity);
        Assert.Equal(expected.Field.AngleOffsetRadians, actual.Field.AngleOffsetRadians);
        Assert.Equal(expected.DetailAnalysis.BaseDetail, actual.DetailAnalysis.BaseDetail);
        Assert.Equal(expected.DetailAnalysis.EdgeWeight, actual.DetailAnalysis.EdgeWeight);
        Assert.Equal(expected.DetailAnalysis.ContrastWeight, actual.DetailAnalysis.ContrastWeight);
        Assert.Equal(expected.DetailAnalysis.SmoothingRadius, actual.DetailAnalysis.SmoothingRadius);
        Assert.Equal(expected.DetailInfluence.PlacementBias, actual.DetailInfluence.PlacementBias);
        Assert.Equal(expected.DetailInfluence.DetailedLengthMultiplier, actual.DetailInfluence.DetailedLengthMultiplier);
        Assert.Equal(expected.DetailInfluence.BackgroundLengthMultiplier, actual.DetailInfluence.BackgroundLengthMultiplier);
        Assert.Equal(expected.DetailInfluence.DetailedWidthMultiplier, actual.DetailInfluence.DetailedWidthMultiplier);
        Assert.Equal(expected.DetailInfluence.BackgroundWidthMultiplier, actual.DetailInfluence.BackgroundWidthMultiplier);
        Assert.Equal(expected.DetailInfluence.RegionTransitionWidth, actual.DetailInfluence.RegionTransitionWidth);
        Assert.Equal(expected.Brush.Kind, actual.Brush.Kind);
        Assert.Equal(expected.Brush.Hardness, actual.Brush.Hardness);
        Assert.Equal(expected.Brush.SizeJitter, actual.Brush.SizeJitter);
        Assert.Equal(expected.Brush.OpacityJitter, actual.Brush.OpacityJitter);
        Assert.Equal(expected.Brush.BristleCount, actual.Brush.BristleCount);
        Assert.Equal(expected.Brush.BristleSpread, actual.Brush.BristleSpread);
        Assert.Equal(expected.SemanticAnalysis.Enabled, actual.SemanticAnalysis.Enabled);
        Assert.Equal(expected.SemanticAnalysis.OverallInfluence, actual.SemanticAnalysis.OverallInfluence);
        Assert.Equal(expected.SemanticAnalysis.SaliencyWeight, actual.SemanticAnalysis.SaliencyWeight);
        Assert.Equal(expected.SemanticAnalysis.SubjectWeight, actual.SemanticAnalysis.SubjectWeight);
        Assert.Equal(expected.SemanticAnalysis.SilhouetteWeight, actual.SemanticAnalysis.SilhouetteWeight);
        Assert.Equal(expected.SemanticAnalysis.FocalWeight, actual.SemanticAnalysis.FocalWeight);
        Assert.Equal(expected.SemanticAnalysis.SubjectThreshold, actual.SemanticAnalysis.SubjectThreshold);
        Assert.Equal(expected.SemanticAnalysis.MinimumSubjectAreaRatio, actual.SemanticAnalysis.MinimumSubjectAreaRatio);
        Assert.Equal(expected.SemanticAnalysis.MaximumSubjects, actual.SemanticAnalysis.MaximumSubjects);
        Assert.Equal(expected.SemanticAnalysis.CenterBias, actual.SemanticAnalysis.CenterBias);
        Assert.Equal(expected.SemanticAnalysis.SmoothingRadius, actual.SemanticAnalysis.SmoothingRadius);
        Assert.Equal(expected.SemanticAnalysis.BoundaryRadius, actual.SemanticAnalysis.BoundaryRadius);
        Assert.Equal(expected.BoundaryAnalysis.Enabled, actual.BoundaryAnalysis.Enabled);
        Assert.Equal(expected.BoundaryAnalysis.LuminanceWeight, actual.BoundaryAnalysis.LuminanceWeight);
        Assert.Equal(expected.BoundaryAnalysis.ColorWeight, actual.BoundaryAnalysis.ColorWeight);
        Assert.Equal(expected.BoundaryAnalysis.MultiscaleWeight, actual.BoundaryAnalysis.MultiscaleWeight);
        Assert.Equal(expected.BoundaryAnalysis.ContinuityWeight, actual.BoundaryAnalysis.ContinuityWeight);
        Assert.Equal(expected.BoundaryAnalysis.SemanticBoundaryWeight, actual.BoundaryAnalysis.SemanticBoundaryWeight);
        Assert.Equal(expected.BoundaryAnalysis.TextureSuppression, actual.BoundaryAnalysis.TextureSuppression);
        Assert.Equal(expected.BoundaryAnalysis.EdgeThreshold, actual.BoundaryAnalysis.EdgeThreshold);
        Assert.Equal(expected.BoundaryAnalysis.ImportantEdgeThreshold, actual.BoundaryAnalysis.ImportantEdgeThreshold);
        Assert.Equal(expected.BoundaryAnalysis.CoarseRadius, actual.BoundaryAnalysis.CoarseRadius);
        Assert.Equal(expected.BoundaryAnalysis.SmoothingRadius, actual.BoundaryAnalysis.SmoothingRadius);
        Assert.Equal(expected.BoundaryAnalysis.BoundaryProtectionRadius, actual.BoundaryAnalysis.BoundaryProtectionRadius);
        Assert.Equal(expected.BoundaryPainting.Enabled, actual.BoundaryPainting.Enabled);
        Assert.Equal(expected.BoundaryPainting.TangentAlignment, actual.BoundaryPainting.TangentAlignment);
        Assert.Equal(expected.BoundaryPainting.AlignmentRadius, actual.BoundaryPainting.AlignmentRadius);
        Assert.Equal(expected.BoundaryPainting.CrossingPenalty, actual.BoundaryPainting.CrossingPenalty);
        Assert.Equal(expected.BoundaryPainting.HardBoundaryThreshold, actual.BoundaryPainting.HardBoundaryThreshold);
        Assert.Equal(expected.BoundaryPainting.TerminationStrength, actual.BoundaryPainting.TerminationStrength);
        Assert.Equal(expected.BoundaryPainting.InternalEdgeInfluence, actual.BoundaryPainting.InternalEdgeInfluence);
        Assert.Equal(expected.BoundaryPainting.TextureEdgeInfluence, actual.BoundaryPainting.TextureEdgeInfluence);
        Assert.Equal(expected.BoundaryPainting.ContourReinforcement, actual.BoundaryPainting.ContourReinforcement);
        Assert.Equal(expected.BoundaryPainting.CornerPreservation, actual.BoundaryPainting.CornerPreservation);
        Assert.Equal(expected.BackgroundSuppression.Enabled, actual.BackgroundSuppression.Enabled);
        Assert.Equal(expected.BackgroundSuppression.OverallStrength, actual.BackgroundSuppression.OverallStrength);
        Assert.Equal(expected.BackgroundSuppression.DetailFloor, actual.BackgroundSuppression.DetailFloor);
        Assert.Equal(expected.BackgroundSuppression.UncertaintyProtection, actual.BackgroundSuppression.UncertaintyProtection);
        Assert.Equal(expected.BackgroundSuppression.SilhouetteProtection, actual.BackgroundSuppression.SilhouetteProtection);
        Assert.Equal(expected.BackgroundSuppression.TransitionSoftness, actual.BackgroundSuppression.TransitionSoftness);
        Assert.Equal(expected.BackgroundSuppression.BackgroundPlacementWeight, actual.BackgroundSuppression.BackgroundPlacementWeight);
        Assert.Equal(expected.BackgroundSuppression.StrokeLengthMultiplier, actual.BackgroundSuppression.StrokeLengthMultiplier);
        Assert.Equal(expected.BackgroundSuppression.StrokeWidthMultiplier, actual.BackgroundSuppression.StrokeWidthMultiplier);
        Assert.Equal(expected.BackgroundSuppression.SegmentMultiplier, actual.BackgroundSuppression.SegmentMultiplier);
        Assert.Equal(expected.BackgroundSuppression.CurveFreedomMultiplier, actual.BackgroundSuppression.CurveFreedomMultiplier);
        Assert.Equal(expected.BackgroundSuppression.ColorSimplification, actual.BackgroundSuppression.ColorSimplification);
    }
}
