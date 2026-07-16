using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Detail;

public static class DetailMapComposer
{
    public static DetailMap ApplyRegions(
        DetailMap source,
        IEnumerable<DetailRegion> regions,
        CancellationToken cancellationToken = default)
    {
        return ApplyRegions(
            source,
            regions,
            DetailInfluenceSettings.DefaultRegionTransitionWidth,
            cancellationToken);
    }

    public static DetailMap ApplyRegions(
        DetailMap source,
        IEnumerable<DetailRegion> regions,
        double transitionWidth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(regions);
        ValidateTransitionWidth(transitionWidth);
        cancellationToken.ThrowIfCancellationRequested();

        DetailRegion[] regionArray = regions.ToArray();
        float[] values = source.CopyValues();
        if (regionArray.Length == 0)
        {
            return new DetailMap(source.Width, source.Height, values);
        }

        float[] increaseInfluence = new float[values.Length];
        float[] reduceInfluence = new float[values.Length];
        int lastIncreaseIndex = -1;
        int lastReduceIndex = -1;

        for (int regionIndex = 0; regionIndex < regionArray.Length; regionIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DetailRegion region = regionArray[regionIndex];
            float[] target = region.Intent switch
            {
                DetailRegionIntent.IncreaseDetail => increaseInfluence,
                DetailRegionIntent.ReduceDetail => reduceInfluence,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(regions),
                    region.Intent,
                    "Unknown detail intent.")
            };

            if (region.Intent == DetailRegionIntent.IncreaseDetail)
            {
                lastIncreaseIndex = regionIndex;
            }
            else
            {
                lastReduceIndex = regionIndex;
            }

            MergeRegionInfluence(
                target,
                source.Width,
                source.Height,
                region,
                transitionWidth,
                cancellationToken);
        }

        // Regions with the same intent merge through their maximum local influence, so
        // overlapping feather bands do not create artificial peaks. Between opposing
        // intents, the intent edited most recently remains the final operation.
        if (lastIncreaseIndex >= 0 && lastReduceIndex >= 0)
        {
            if (lastIncreaseIndex < lastReduceIndex)
            {
                ApplyIncrease(values, increaseInfluence, cancellationToken);
                ApplyReduction(values, reduceInfluence, cancellationToken);
            }
            else
            {
                ApplyReduction(values, reduceInfluence, cancellationToken);
                ApplyIncrease(values, increaseInfluence, cancellationToken);
            }
        }
        else if (lastIncreaseIndex >= 0)
        {
            ApplyIncrease(values, increaseInfluence, cancellationToken);
        }
        else
        {
            ApplyReduction(values, reduceInfluence, cancellationToken);
        }

        return new DetailMap(source.Width, source.Height, values);
    }

    private static void MergeRegionInfluence(
        float[] target,
        int width,
        int height,
        DetailRegion region,
        double transitionWidth,
        CancellationToken cancellationToken)
    {
        double transitionPixels = transitionWidth * Math.Min(width, height);
        double left = region.Bounds.Left * width;
        double top = region.Bounds.Top * height;
        double right = region.Bounds.Right * width;
        double bottom = region.Bounds.Bottom * height;

        int minimumX = Math.Clamp(
            (int)Math.Floor(left - transitionPixels),
            0,
            width - 1);
        int maximumX = Math.Clamp(
            (int)Math.Ceiling(right + transitionPixels) - 1,
            0,
            width - 1);
        int minimumY = Math.Clamp(
            (int)Math.Floor(top - transitionPixels),
            0,
            height - 1);
        int maximumY = Math.Clamp(
            (int)Math.Ceiling(bottom + transitionPixels) - 1,
            0,
            height - 1);

        for (int y = minimumY; y <= maximumY; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double pixelY = y + 0.5d;

            for (int x = minimumX; x <= maximumX; x++)
            {
                double pixelX = x + 0.5d;
                double feather = CalculateFeatherInfluence(
                    pixelX,
                    pixelY,
                    left,
                    top,
                    right,
                    bottom,
                    transitionPixels);
                if (feather <= 0d)
                {
                    continue;
                }

                float influence = (float)(region.Strength * feather);
                int index = checked((y * width) + x);
                target[index] = Math.Max(target[index], influence);
            }
        }
    }

    private static double CalculateFeatherInfluence(
        double x,
        double y,
        double left,
        double top,
        double right,
        double bottom,
        double transitionPixels)
    {
        bool inside = x >= left && x <= right && y >= top && y <= bottom;
        if (transitionPixels <= 0d)
        {
            return inside ? 1d : 0d;
        }

        if (inside)
        {
            double distanceToBoundary = Math.Min(
                Math.Min(x - left, right - x),
                Math.Min(y - top, bottom - y));
            double maximumInnerDistance = Math.Min(
                (right - left) * 0.5d,
                (bottom - top) * 0.5d);
            double innerTransition = Math.Min(transitionPixels, maximumInnerDistance);
            if (innerTransition <= 0d)
            {
                return 1d;
            }

            double amount = Math.Clamp(distanceToBoundary / innerTransition, 0d, 1d);
            return 0.5d + (0.5d * SmoothStep(amount));
        }

        double distanceX = x < left
            ? left - x
            : x > right
                ? x - right
                : 0d;
        double distanceY = y < top
            ? top - y
            : y > bottom
                ? y - bottom
                : 0d;
        double distanceOutside = Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
        double outsideAmount = Math.Clamp(distanceOutside / transitionPixels, 0d, 1d);
        return 0.5d * (1d - SmoothStep(outsideAmount));
    }

    private static void ApplyIncrease(
        float[] values,
        float[] influence,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < values.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float amount = influence[index];
            if (amount > 0f)
            {
                float current = values[index];
                values[index] = current + (amount * (1f - current));
            }
        }
    }

    private static void ApplyReduction(
        float[] values,
        float[] influence,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < values.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float amount = influence[index];
            if (amount > 0f)
            {
                values[index] *= 1f - amount;
            }
        }
    }

    private static double SmoothStep(double amount)
    {
        return amount * amount * (3d - (2d * amount));
    }

    private static void ValidateTransitionWidth(double transitionWidth)
    {
        if (!double.IsFinite(transitionWidth) || transitionWidth < 0d || transitionWidth > 0.5d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transitionWidth),
                transitionWidth,
                "Transition width must be finite and between 0 and 0.5 of the shorter map dimension.");
        }
    }
}
