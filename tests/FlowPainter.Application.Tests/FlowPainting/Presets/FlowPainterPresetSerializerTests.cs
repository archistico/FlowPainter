using System.Text;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
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
                new DetailInfluenceSettings(6d, 0.4d, 1.6d, 0.5d, 1.7d)));
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
    public async Task DeserializeRejectsUnsupportedSchemaVersion()
    {
        await using MemoryStream valid = new();
        await FlowPainterPresetSerializer.SerializeAsync(
            new FlowPainterPreset("Invalid version", new FlowPainterSettings(strokeCount: 1)),
            valid);
        string json = Encoding.UTF8.GetString(valid.ToArray())
            .Replace("\"schemaVersion\": 2", "\"schemaVersion\": 99", StringComparison.Ordinal);
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
    }
}
