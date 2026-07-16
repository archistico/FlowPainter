using System.Text;
using System.Text.Json.Nodes;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Projects;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.Projects;

public sealed class PrimitiveProjectPersistenceTests
{
    [Fact]
    public async Task RoundTripPreservesPrimitiveModeAndSettings()
    {
        PrimitiveGenerationSettings primitiveSettings = new(
            primitiveCount: 456,
            candidatesPerStep: 18,
            mutationIterations: 31,
            minimumSize: 0.02d,
            maximumSize: 0.35d,
            opacity: 0.61d,
            detailSizeInfluence: 0.55d,
            detailPlacementBias: 4d,
            detailErrorWeight: 2.5d,
            detailSearchInfluence: 1.75d,
            allowedKinds: PrimitiveKindSet.Triangle | PrimitiveKindSet.Ellipse);
        FlowPainterProject expected = new(
            "Primitive study",
            "source.png",
            77UL,
            new FlowPainterSettings(),
            mode: GenerativeMode.GeometricPrimitives,
            primitiveGeneration: primitiveSettings);
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(expected, stream);
        stream.Position = 0L;
        FlowPainterProject actual = await FlowPainterProjectSerializer.DeserializeAsync(stream);

        Assert.Equal(GenerativeMode.GeometricPrimitives, actual.Mode);
        Assert.Equal(456, actual.PrimitiveGeneration.PrimitiveCount);
        Assert.Equal(18, actual.PrimitiveGeneration.CandidatesPerStep);
        Assert.Equal(PrimitiveKindSet.Triangle | PrimitiveKindSet.Ellipse, actual.PrimitiveGeneration.AllowedKinds);
        Assert.Equal(0.61d, actual.PrimitiveGeneration.Opacity, 12);
        Assert.Equal(1.75d, actual.PrimitiveGeneration.DetailSearchInfluence, 12);
    }

    [Fact]
    public async Task DeserializeSchemaFourUsesFlowModeAndDefaultPrimitiveSettings()
    {
        FlowPainterProject project = new(
            "Legacy flow",
            "source.png",
            1UL,
            new FlowPainterSettings(),
            mode: GenerativeMode.GeometricPrimitives,
            primitiveGeneration: new PrimitiveGenerationSettings(primitiveCount: 999));
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 4;
        JsonObject payload = root["project"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no project payload.");
        payload.Remove("mode");
        payload.Remove("primitiveGeneration");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.Equal(GenerativeMode.FlowPainting, loaded.Mode);
        Assert.Equal(300, loaded.PrimitiveGeneration.PrimitiveCount);
        Assert.Equal(PrimitiveKindSet.All, loaded.PrimitiveGeneration.AllowedKinds);
    }
}
