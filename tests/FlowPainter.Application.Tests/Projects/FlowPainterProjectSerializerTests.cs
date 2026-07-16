using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;
using FlowPainter.Domain.Semantics;

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
            detailInfluence: new DetailInfluenceSettings(5.5d, 0.4d, 1.6d, 0.55d, 1.7d, 0.08d),
            brush: new BrushSettings(BrushKind.Flat, 0.7d, 0.12d, 0.2d, 5, 0.65d),
            semanticAnalysis: new SemanticAnalysisSettings(
                enabled: true,
                overallInfluence: 0.55d,
                saliencyWeight: 0.4d,
                subjectWeight: 1.2d,
                silhouetteWeight: 1.1d,
                focalWeight: 1.4d,
                subjectThreshold: 0.45d,
                minimumSubjectAreaRatio: 0.008d,
                maximumSubjects: 4,
                centerBias: 0.6d,
                smoothingRadius: 1,
                boundaryRadius: 3),
            boundaryAnalysis: new SceneBoundaryAnalysisSettings(
                enabled: true,
                luminanceWeight: 0.6d,
                colorWeight: 0.9d,
                multiscaleWeight: 0.8d,
                continuityWeight: 1.1d,
                semanticBoundaryWeight: 1.5d,
                textureSuppression: 0.7d,
                edgeThreshold: 0.12d,
                importantEdgeThreshold: 0.38d,
                coarseRadius: 4,
                smoothingRadius: 2,
                boundaryProtectionRadius: 5));
        DetailRegion region = new(
            "manual-0007",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.7d),
            0.75d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Eyes");
        SemanticCorrectionRegion correction = new(
            "semantic-correction-0003",
            new NormalizedRect(0.2d, 0.1d, 0.8d, 0.9d),
            SemanticCorrectionKind.ForcePrimarySubject,
            "Main portrait",
            "semantic-subject-01");
        FlowPainterProject expected = new(
            "Portrait",
            "images/portrait.png",
            987654UL,
            settings,
            new PreviewSettings(PreviewQuality.High),
            [region],
            new FinalRenderSettings(7000, RasterImageFormat.Jpeg, 87),
            semanticCorrections: [correction]);
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
        SemanticCorrectionRegion loadedCorrection = Assert.Single(actual.SemanticCorrections);
        Assert.Equal(correction, loadedCorrection);
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
    public async Task DeserializeMigratesSchemaVersionTwoWithDefaultBrush()
    {
        FlowPainterProject project = CreateProject();
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 2;
        JsonObject settings = root["project"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no settings.");
        settings.Remove("brush");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.Equal(BrushKind.SolidRound, loaded.Settings.Brush.Kind);
        Assert.Equal(BrushSettings.DefaultHardness, loaded.Settings.Brush.Hardness);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionThreeWithSemanticDefaults()
    {
        FlowPainterProject project = CreateProject();
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 3;
        JsonObject settings = root["project"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no settings.");
        settings.Remove("semanticAnalysis");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.True(loaded.Settings.SemanticAnalysis.Enabled);
        Assert.Equal(
            SemanticAnalysisSettings.DefaultOverallInfluence,
            loaded.Settings.SemanticAnalysis.OverallInfluence);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionSixWithBoundaryDefaults()
    {
        FlowPainterProject project = CreateProject();
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 6;
        JsonObject settings = root["project"]?["settings"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no settings.");
        settings.Remove("boundaryAnalysis");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.True(loaded.Settings.BoundaryAnalysis.Enabled);
        Assert.Equal(
            SceneBoundaryAnalysisSettings.DefaultSemanticBoundaryWeight,
            loaded.Settings.BoundaryAnalysis.SemanticBoundaryWeight);
    }

    [Fact]
    public async Task DeserializeMigratesSchemaVersionNineWithDefaultRegionTransition()
    {
        FlowPainterProject project = new(
            "Legacy soft regions",
            "images/source.png",
            42UL,
            new FlowPainterSettings(
                detailInfluence: new DetailInfluenceSettings(regionTransitionWidth: 0.12d)));
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;

        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 9;
        JsonObject detailInfluence = root["project"]?["settings"]?["detailInfluence"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project has no detail-influence settings.");
        detailInfluence.Remove("regionTransitionWidth");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.Equal(
            DetailInfluenceSettings.DefaultRegionTransitionWidth,
            loaded.Settings.DetailInfluence.RegionTransitionWidth);
    }

    [Fact]
    public async Task DeserializeSchemaVersionTenDefaultsSemanticCorrectionsToEmpty()
    {
        FlowPainterProject project = CreateProject();
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 10;
        root["project"]?.AsObject().Remove("semanticCorrections");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.Empty(loaded.SemanticCorrections);
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

    private static FlowPainterProject CreateProject()
    {
        return new FlowPainterProject("Project", "source.png", 42UL, new FlowPainterSettings());
    }

    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
