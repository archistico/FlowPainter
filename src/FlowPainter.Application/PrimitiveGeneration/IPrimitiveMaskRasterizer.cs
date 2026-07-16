using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.PrimitiveGeneration;

public interface IPrimitiveMaskRasterizer
{
    PrimitiveRasterMask Rasterize(GeometricPrimitive primitive, ImageSize size);
}
