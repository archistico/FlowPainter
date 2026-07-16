using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.PrimitiveGeneration;

public interface IPrimitiveScorer
{
    PrimitiveScore Score(
        ImageSize size,
        ReadOnlyMemory<Rgba32> sourcePixels,
        ReadOnlyMemory<Rgba32> currentPixels,
        DetailMap? detailMap,
        GeometricPrimitive candidate,
        PrimitiveGenerationSettings settings);
}
