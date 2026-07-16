using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Semantics;

public static class SemanticCorrectionComposer
{
    public static SemanticAnalysisResult Apply(
        SemanticAnalysisResult source,
        IEnumerable<SemanticCorrectionRegion> corrections,
        double transitionWidth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(corrections);
        ValidateTransitionWidth(transitionWidth);
        cancellationToken.ThrowIfCancellationRequested();

        SemanticCorrectionRegion[] correctionArray = corrections.ToArray();
        if (correctionArray.Length == 0)
        {
            return source;
        }

        int width = source.ImportanceMap.Width;
        int height = source.ImportanceMap.Height;
        int length = checked(width * height);
        float[] primaryInfluence = new float[length];
        float[] subjectInfluence = new float[length];
        float[] backgroundInfluence = new float[length];
        float[] ignoreInfluence = new float[length];

        foreach (SemanticCorrectionRegion correction in correctionArray)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float[] target = correction.Kind switch
            {
                SemanticCorrectionKind.ForcePrimarySubject => primaryInfluence,
                SemanticCorrectionKind.ForceSubject => subjectInfluence,
                SemanticCorrectionKind.ForceBackground => backgroundInfluence,
                SemanticCorrectionKind.IgnoreAutomaticDetection => ignoreInfluence,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(corrections),
                    correction.Kind,
                    "Unknown semantic-correction kind.")
            };
            MergeRegionInfluence(
                target,
                width,
                height,
                correction,
                transitionWidth,
                cancellationToken);
        }

        float[] saliency = source.SaliencyMap.CopyValues();
        float[] subjects = source.SubjectMap.CopyValues();
        float[] silhouettes = source.SilhouetteMap.CopyValues();
        float[] focal = source.FocalMap.CopyValues();
        float[] importance = source.ImportanceMap.CopyValues();

        for (int index = 0; index < length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float subject = subjectInfluence[index];
            float background = backgroundInfluence[index];
            float ignore = ignoreInfluence[index];
            float primary = primaryInfluence[index];

            // Explicit precedence is applied from lowest to highest:
            // subject promotion, exclusion/background, then primary subject.
            if (subject > 0f)
            {
                subjects[index] = Promote(subjects[index], subject);
                importance[index] = Promote(importance[index], subject * 0.85f);
            }

            if (background > 0f)
            {
                float retention = 1f - background;
                saliency[index] *= retention;
                subjects[index] *= retention;
                silhouettes[index] *= retention;
                focal[index] *= retention;
                importance[index] *= retention;
            }

            if (ignore > 0f)
            {
                float retention = 1f - ignore;
                subjects[index] *= retention;
                silhouettes[index] *= retention;
                focal[index] *= retention;
                importance[index] *= retention;
            }

            if (primary > 0f)
            {
                subjects[index] = Promote(subjects[index], primary);
                focal[index] = Promote(focal[index], primary);
                importance[index] = Promote(importance[index], primary);
            }
        }

        return new SemanticAnalysisResult(
            new DetailMap(width, height, saliency),
            new DetailMap(width, height, subjects),
            new DetailMap(width, height, silhouettes),
            new DetailMap(width, height, focal),
            new DetailMap(width, height, importance),
            source.Regions,
            $"{source.ProviderId}+manual-corrections");
    }

    private static float Promote(float current, float influence)
    {
        return current + (influence * (1f - current));
    }

    private static void MergeRegionInfluence(
        float[] target,
        int width,
        int height,
        SemanticCorrectionRegion correction,
        double transitionWidth,
        CancellationToken cancellationToken)
    {
        double transitionPixels = transitionWidth * Math.Min(width, height);
        double left = correction.Bounds.Left * width;
        double top = correction.Bounds.Top * height;
        double right = correction.Bounds.Right * width;
        double bottom = correction.Bounds.Bottom * height;

        int minimumX = Math.Clamp((int)Math.Floor(left - transitionPixels), 0, width - 1);
        int maximumX = Math.Clamp((int)Math.Ceiling(right + transitionPixels) - 1, 0, width - 1);
        int minimumY = Math.Clamp((int)Math.Floor(top - transitionPixels), 0, height - 1);
        int maximumY = Math.Clamp((int)Math.Ceiling(bottom + transitionPixels) - 1, 0, height - 1);

        for (int y = minimumY; y <= maximumY; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double pixelY = y + 0.5d;
            for (int x = minimumX; x <= maximumX; x++)
            {
                double influence = CalculateFeatherInfluence(
                    x + 0.5d,
                    pixelY,
                    left,
                    top,
                    right,
                    bottom,
                    transitionPixels);
                if (influence <= 0d)
                {
                    continue;
                }

                int index = checked((y * width) + x);
                target[index] = Math.Max(target[index], (float)influence);
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
