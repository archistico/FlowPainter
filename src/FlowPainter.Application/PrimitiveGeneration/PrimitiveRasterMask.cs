using FlowPainter.Domain.Images;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class PrimitiveRasterMask
{
    private readonly IReadOnlyList<int> _pixelIndices;

    public PrimitiveRasterMask(ImageSize size, IEnumerable<int> pixelIndices)
    {
        ArgumentNullException.ThrowIfNull(pixelIndices);
        int[] copied = pixelIndices.ToArray();
        for (int index = 0; index < copied.Length; index++)
        {
            if (copied[index] < 0 || copied[index] >= size.PixelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pixelIndices),
                    copied[index],
                    "Primitive mask pixel indexes must lie inside the image.");
            }
        }

        Size = size;
        _pixelIndices = Array.AsReadOnly(copied);
    }

    public ImageSize Size { get; }

    public IReadOnlyList<int> PixelIndices => _pixelIndices;
}
