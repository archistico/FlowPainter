using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Images;

public interface IRgbaPixelSource
{
    ImageSize Size { get; }

    Rgba32 SampleNearest(NormalizedPoint point);
}
