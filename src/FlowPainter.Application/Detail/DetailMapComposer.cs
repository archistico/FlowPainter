using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Detail;

public static class DetailMapComposer
{
    public static DetailMap ApplyRegions(
        DetailMap source,
        IEnumerable<DetailRegion> regions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(regions);

        float[] values = source.CopyValues();

        foreach (DetailRegion region in regions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyRegion(values, source.Width, source.Height, region, cancellationToken);
        }

        return new DetailMap(source.Width, source.Height, values);
    }

    private static void ApplyRegion(
        float[] values,
        int width,
        int height,
        DetailRegion region,
        CancellationToken cancellationToken)
    {
        int minimumX = Math.Clamp((int)Math.Floor(region.Bounds.Left * width), 0, width - 1);
        int maximumX = Math.Clamp((int)Math.Ceiling(region.Bounds.Right * width) - 1, 0, width - 1);
        int minimumY = Math.Clamp((int)Math.Floor(region.Bounds.Top * height), 0, height - 1);
        int maximumY = Math.Clamp((int)Math.Ceiling(region.Bounds.Bottom * height) - 1, 0, height - 1);

        for (int y = minimumY; y <= maximumY; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double normalizedY = (y + 0.5d) / height;

            for (int x = minimumX; x <= maximumX; x++)
            {
                double normalizedX = (x + 0.5d) / width;
                if (!region.Bounds.Contains(new NormalizedPoint(normalizedX, normalizedY)))
                {
                    continue;
                }

                int index = checked((y * width) + x);
                float current = values[index];
                float strength = (float)region.Strength;

                values[index] = region.Intent switch
                {
                    DetailRegionIntent.IncreaseDetail => current + (strength * (1f - current)),
                    DetailRegionIntent.ReduceDetail => current * (1f - strength),
                    _ => throw new ArgumentOutOfRangeException(nameof(region), region.Intent, "Unknown detail intent.")
                };
            }
        }
    }
}
