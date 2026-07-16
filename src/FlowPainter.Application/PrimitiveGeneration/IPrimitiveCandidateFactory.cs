using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.PrimitiveGeneration;

public interface IPrimitiveCandidateFactory
{
    GeometricPrimitive Create(
        int index,
        DetailMap? detailMap,
        PrimitiveGenerationSettings settings,
        IRandomSource random);
}
