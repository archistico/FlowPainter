using System.Text;
using System.Text.Json.Nodes;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.Projects;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Hybrid;

namespace FlowPainter.Application.Tests.Projects;

public sealed class HybridProjectPersistenceTests
{
    [Fact]
    public async Task RoundTripPreservesHybridModeAndSettings()
    {
        HybridGenerationSettings hybrid = new(
            primitiveBudgetFraction: 0.25d,
            flowBudgetFraction: 0.50d,
            refinementBudgetFraction: 0.25d,
            influenceKind: PrimitiveFlowInfluenceKind.BoundaryTangent,
            influenceStrength: 0.72d,
            influenceRadiusMultiplier: 2.4d,
            maximumInfluencesPerSample: 9,
            refinementDetailBias: 5d,
            refinementLengthMultiplier: 0.35d,
            refinementWidthMultiplier: 0.42d);
        FlowPainterProject expected = new(
            "Hybrid study",
            "source.png",
            55UL,
            new FlowPainterSettings(),
            mode: GenerativeMode.Hybrid,
            hybridGeneration: hybrid);
        await using MemoryStream stream = new();

        await FlowPainterProjectSerializer.SerializeAsync(expected, stream);
        stream.Position = 0L;
        FlowPainterProject actual = await FlowPainterProjectSerializer.DeserializeAsync(stream);

        Assert.Equal(GenerativeMode.Hybrid, actual.Mode);
        Assert.Equal(0.25d, actual.HybridGeneration.PrimitiveBudgetFraction, 12);
        Assert.Equal(0.50d, actual.HybridGeneration.FlowBudgetFraction, 12);
        Assert.Equal(PrimitiveFlowInfluenceKind.BoundaryTangent, actual.HybridGeneration.InfluenceKind);
        Assert.Equal(2.4d, actual.HybridGeneration.InfluenceRadiusMultiplier, 12);
        Assert.Equal(9, actual.HybridGeneration.MaximumInfluencesPerSample);
        Assert.Equal(0.42d, actual.HybridGeneration.RefinementWidthMultiplier, 12);
    }

    [Fact]
    public async Task DeserializeSchemaFiveUsesDefaultHybridSettings()
    {
        FlowPainterProject project = new(
            "Legacy primitive",
            "source.png",
            1UL,
            new FlowPainterSettings(),
            mode: GenerativeMode.GeometricPrimitives,
            hybridGeneration: new HybridGenerationSettings(
                influenceKind: PrimitiveFlowInfluenceKind.Vortex));
        await using MemoryStream current = new();
        await FlowPainterProjectSerializer.SerializeAsync(project, current);
        current.Position = 0L;
        JsonObject root = (await JsonNode.ParseAsync(current))?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON is empty.");
        root["schemaVersion"] = 5;
        JsonObject payload = root["project"]?.AsObject()
            ?? throw new InvalidOperationException("The serialized project JSON has no project payload.");
        payload.Remove("hybridGeneration");
        await using MemoryStream legacy = new(Encoding.UTF8.GetBytes(root.ToJsonString()));

        FlowPainterProject loaded = await FlowPainterProjectSerializer.DeserializeAsync(legacy);

        Assert.Equal(PrimitiveFlowInfluenceKind.Mixed, loaded.HybridGeneration.InfluenceKind);
        Assert.Equal(HybridGenerationSettings.DefaultInfluenceStrength, loaded.HybridGeneration.InfluenceStrength, 12);
        Assert.Equal(HybridGenerationSettings.DefaultRefinementWidthMultiplier, loaded.HybridGeneration.RefinementWidthMultiplier, 12);
    }
}
