using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.PrimitiveGeneration;

public interface IPrimitiveMutator
{
    GeometricPrimitive Mutate(
        GeometricPrimitive primitive,
        PrimitiveGenerationSettings settings,
        IRandomSource random);
}
