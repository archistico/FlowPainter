using FlowPainter.Domain.Images;

namespace FlowPainter.Application.FlowPainting.Legacy;

public static class LegacyDensityMapFactory
{
    public static LegacyDensityMap CreateUniform(ImageSize size, double density)
    {
        if (!double.IsFinite(density) || density <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(density), density, "Density must be finite and greater than zero.");
        }

        double[] values = new double[checked((int)size.PixelCount)];
        Array.Fill(values, density);
        return new LegacyDensityMap(size, values);
    }
}
