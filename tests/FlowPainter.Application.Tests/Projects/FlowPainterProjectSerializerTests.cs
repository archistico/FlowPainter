using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Projects;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.Projects;

public sealed class FlowPainterProjectSerializerTests
{
    [Fact]
    public async Task RoundTripPreservesProjectData()
    {
        FlowPainterSettings settings = new(
            new FlowFieldSettings(FlowFieldKind.CoherentNoise, 4.25d, 3, 0.6d, 2.2d, 0.15d),
            strokeCount: 4321,
            segmentCount: 18,
            referenceMaximumDimension: 1024,
            uniformDensity: 14.5d,
            lengthScale: 0.0075d,
            maximumCurveRadians: 0.8d,
            minimumStrokeWidthPixels: 2.25d,
            maximumStrokeWidthPixels: 8.75d,
            strokeOpacity: 0.63d,
            backgroundMode: StrokePlanBackgroundMode.Transparent,
            detailAnalysis: new DetailAnalysisSettings(0.2d, 0.9d, 0.6d, 3),
            detailInfluence: new DetailInfluenceSettings(5.5d, 0.4d, 1.6d, 0.55d, 1.7d));
        DetailRegion region = new(
            "manual-0007",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.7d),
            0.75d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Eyes");
        FlowPainterProject expected = new(
            "Portrait",
            "images/portrait.png",
            987654UL,
            settings,
            new PreviewSettings(PreviewQuality.High),
            [region],
            new FinalRenderSettings(7000, RasterImageFormat.Jpeg, 87));
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(expected, stream);
        stream.Position = 0L;
        FlowPainterProject actual = await FlowPainterProjectSerializer.DeserializeAsync(stream);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SourcePath, actual.SourcePath);
        Assert.Equal(expected.Seed, actual.Seed);
        Assert.Equal(expected.Preview.Quality, actual.Preview.Quality);
        Assert.Equal(expected.FinalRender.MaximumDimension, actual.FinalRender.MaximumDimension);
        Assert.Equal(expected.FinalRender.Format, actual.FinalRender.Format);
        Assert.Equal(expected.FinalRender.JpegQuality, actual.FinalRender.JpegQuality);
        AssertSettingsEqual(expected.Settings, actual.Settings);
        DetailRegion loadedRegion = Assert.Single(actual.DetailRegions);
        Assert.Equal(region, loadedRegion);
    }


    [Fact]
    public async Task SerializeWritesExplicitRectangleEdgesOnly()
    {
        DetailRegion region = new(
            "manual-0001",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.7d),
            0.75d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Eyes");
        FlowPainterProject project = new(
            "Portrait",
            "images/portrait.png",
            42UL,
            new FlowPainterSettings(),
            detailRegions: [region]);
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(project, stream);
        stream.Position = 0L;
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        JsonElement bounds = document.RootElement
            .GetProperty("project")
            .GetProperty("detailRegions")[0]
            .GetProperty("bounds");

        Assert.Equal(0.1d, bounds.GetProperty("left").GetDouble());
        Assert.Equal(0.2d, bounds.GetProperty("top").GetDouble());
        Assert.Equal(0.4d, bounds.GetProperty("right").GetDouble());
        Assert.Equal(0.7d, bounds.GetProperty("bottom").GetDouble());
        Assert.False(bounds.TryGetProperty("width", out _));
        Assert.False(bounds.TryGetProperty("height", out _));
    }

    [Fact]
    public async Task DeserializeAcceptsPreviousRectanglePayloadWithDerivedDimensions()
    {
        DetailRegion region = new(
            "manual-0001",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.7d),
            0.75d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Eyes");
        FlowPainterProject project = new(
            "Portrait",
            "images/portrait.png",
            42UL,
            new FlowPainterSettings(),
            detailRegions: [region]);
        await using MemoryStream currentStream = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, currentStream);
        currentStream.Position = 0L;

        JsonObject root = (await JsonNode.ParseAsync(currentStream))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        JsonObject bounds = root["project"]?["detailRegions"]?[0]?["bounds"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON does not contain region bounds.");
        bounds["width"] = 0.3d;
        bounds["height"] = 0.5d;
        await using MemoryStream previousStream = new(
            Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(previousStream);

        Assert.Equal(region, Assert.Single(loaded.DetailRegions));
    }

    [Fact]
    public async Task DeserializeRejectsInvalidRectangleBounds()
    {
        DetailRegion region = new(
            "manual-0001",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.7d),
            0.75d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Eyes");
        FlowPainterProject project = new(
            "Portrait",
            "images/portrait.png",
            42UL,
            new FlowPainterSettings(),
            detailRegions: [region]);
        await using MemoryStream validStream = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, validStream);
        validStream.Position = 0L;

        JsonObject root = (await JsonNode.ParseAsync(validStream))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        JsonObject bounds = root["project"]?["detailRegions"]?[0]?["bounds"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON does not contain region bounds.");
        bounds["right"] = 0.1d;
        await using MemoryStream invalidStream = new(
            Encoding.UTF8.GetBytes(root.ToJsonString()));

        await Assert.ThrowsAsync<JsonException>(() => FlowPainterProjectSerializer.DeserializeAsync(invalidStream));
    }

    [Fact]
    public async Task SerializeTruncatesExistingSeekableStream()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(new string('x', 10_000)));
        FlowPainterProject project = CreateProject();

        await FlowPainterProjectSerializer.SerializeAsync(project, stream);

        Assert.True(stream.Length < 10_000L);
    }

    [Fact]
    public async Task SerializeRejectsReadOnlyStream()
    {
        await using MemoryStream stream = new([], writable: false);

        await Assert.ThrowsAsync<ArgumentException>(() => FlowPainterProjectSerializer.SerializeAsync(CreateProject(), stream));
    }

    [Fact]
    public async Task DeserializeRejectsWriteOnlyStream()
    {
        await using WriteOnlyStream stream = new();

        await Assert.ThrowsAsync<ArgumentException>(() => FlowPainterProjectSerializer.DeserializeAsync(stream));
    }

    [Fact]
    public async Task DeserializeRejectsMissingSchemaVersion()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{\"project\":{}}"));

        await Assert.ThrowsAsync<InvalidDataException>(() => FlowPainterProjectSerializer.DeserializeAsync(stream));
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionOneWithDefaultFinalRenderSettings()
    {
        FlowPainterProject project = CreateProject();
        await using MemoryStream currentStream = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, currentStream);
        currentStream.Position = 0L;

        JsonObject root = (await JsonNode.ParseAsync(currentStream))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 1;
        JsonObject projectObject = root["project"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON does not contain a project payload.");
        projectObject.Remove("finalRender");
        await using MemoryStream legacyStream = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacyStream);

        Assert.Equal(FinalRenderSettings.DefaultMaximumDimension, loaded.FinalRender.MaximumDimension);
        Assert.Equal(RasterImageFormat.Png, loaded.FinalRender.Format);
        Assert.Equal(FinalRenderSettings.DefaultJpegQuality, loaded.FinalRender.JpegQuality);
    }

    [Fact]
    public async Task DeserializeRejectsUnsupportedSchemaVersion()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{\"schemaVersion\":99,\"project\":{}}"));

        await Assert.ThrowsAsync<NotSupportedException>(() => FlowPainterProjectSerializer.DeserializeAsync(stream));
    }

    [Fact]
    public async Task SerializeHonorsCancellation()
    {
        await using MemoryStream stream = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FlowPainterProjectSerializer.SerializeAsync(
            CreateProject(),
            stream,
            cancellation.Token));
    }

    [Fact]
    public async Task DeserializeHonorsCancellation()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{}"));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FlowPainterProjectSerializer.DeserializeAsync(
            stream,
            cancellation.Token));
    }

    private static void AssertSettingsEqual(
        FlowPainterSettings expected,
        FlowPainterSettings actual)
    {
        Assert.Equal(expected.Field.Kind, actual.Field.Kind);
        Assert.Equal(expected.Field.Scale, actual.Field.Scale);
        Assert.Equal(expected.Field.Octaves, actual.Field.Octaves);
        Assert.Equal(expected.Field.Persistence, actual.Field.Persistence);
        Assert.Equal(expected.Field.Lacunarity, actual.Field.Lacunarity);
        Assert.Equal(expected.Field.AngleOffsetRadians, actual.Field.AngleOffsetRadians);
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

    private static FlowPainterProject CreateProject()
    {
        return new FlowPainterProject("Project", "source.png", 42UL, new FlowPainterSettings());
    }

    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
