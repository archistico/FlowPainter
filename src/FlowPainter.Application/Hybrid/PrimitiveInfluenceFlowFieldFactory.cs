using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Hybrid;

public sealed class PrimitiveInfluenceFlowFieldFactory : IFlowFieldFactory
{
    private readonly IFlowFieldFactory _baseFactory;
    private readonly PrimitivePlan _primitivePlan;
    private readonly HybridGenerationSettings _settings;

    public PrimitiveInfluenceFlowFieldFactory(
        IFlowFieldFactory baseFactory,
        PrimitivePlan primitivePlan,
        HybridGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(baseFactory);
        ArgumentNullException.ThrowIfNull(primitivePlan);
        ArgumentNullException.ThrowIfNull(settings);
        _baseFactory = baseFactory;
        _primitivePlan = primitivePlan;
        _settings = settings;
    }

    public IFlowField Create(int seed, FlowFieldSettings settings)
    {
        return new PrimitiveInfluenceFlowField(
            _baseFactory.Create(seed, settings),
            _primitivePlan,
            _settings);
    }
}
