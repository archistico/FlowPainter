namespace FlowPainter.Application.Segmentation;

public static class RegionMergeIntensityMapper
{
    public const double MinimumPercentage = 0d;
    public const double MaximumPercentage = 100d;
    public const double DefaultPercentage = 50d;

    public static RegionMergeSettings Create(double percentage)
    {
        ValidatePercentage(percentage);
        if (percentage == DefaultPercentage)
        {
            return new RegionMergeSettings();
        }

        double amount = percentage / MaximumPercentage;
        return new RegionMergeSettings(
            intermediateTargetRatio: 0.90d - (0.60d * amount),
            broadMassTargetRatio: 0.55d - (0.50d * amount),
            intermediateMaximumCost: 0.22d + (0.40d * amount),
            broadMassMaximumCost: 0.42d + (0.40d * amount),
            strongBoundaryThreshold: 0.90d - (0.36d * amount),
            maximumParentAreaFraction: 0.25d + (0.40d * amount));
    }

    public static double EstimatePercentage(RegionMergeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (MatchesDefaults(settings))
        {
            return DefaultPercentage;
        }

        double amount = (settings.IntermediateMaximumCost - 0.22d) / 0.40d;
        return Math.Clamp(amount * MaximumPercentage, MinimumPercentage, MaximumPercentage);
    }

    private static bool MatchesDefaults(RegionMergeSettings settings)
    {
        return settings.IntermediateTargetRatio == RegionMergeSettings.DefaultIntermediateTargetRatio
            && settings.BroadMassTargetRatio == RegionMergeSettings.DefaultBroadMassTargetRatio
            && settings.IntermediateMaximumCost == RegionMergeSettings.DefaultIntermediateMaximumCost
            && settings.BroadMassMaximumCost == RegionMergeSettings.DefaultBroadMassMaximumCost
            && settings.StrongBoundaryThreshold == RegionMergeSettings.DefaultStrongBoundaryThreshold
            && settings.MaximumParentAreaFraction == RegionMergeSettings.DefaultMaximumParentAreaFraction;
    }

    private static void ValidatePercentage(double percentage)
    {
        if (!double.IsFinite(percentage)
            || percentage < MinimumPercentage
            || percentage > MaximumPercentage)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percentage),
                percentage,
                $"Merge intensity must be finite and between {MinimumPercentage} and {MaximumPercentage} percent.");
        }
    }
}
