using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Domain.Brushes;

namespace FlowPainter.Application.Tests.FlowPainting.Presets;

public sealed class FlowPainterPresetTests
{
    [Fact]
    public void ConstructorTrimsNameAndRetainsSettings()
    {
        FlowPainterSettings settings = new();

        FlowPainterPreset preset = new("  Test preset  ", settings);

        Assert.Equal("Test preset", preset.Name);
        Assert.Same(settings, preset.Settings);
        Assert.Equal(preset.Name, preset.ToString());
    }

    [Fact]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new FlowPainterPreset(" ", new FlowPainterSettings()));
    }

    [Fact]
    public void BuiltInCatalogDemonstratesEveryBrushKind()
    {
        BrushKind[] kinds = BuiltInFlowPainterPresets.All
            .Select(preset => preset.Settings.Brush.Kind)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal(Enum.GetValues<BrushKind>(), kinds);
    }

    [Fact]
    public void BuiltInCatalogEnablesBoundaryGuidanceExceptForLegacyComparison()
    {
        FlowPainterPreset legacy = Assert.Single(
            BuiltInFlowPainterPresets.All,
            preset => string.Equals(preset.Name, "Legacy comparison", StringComparison.Ordinal));

        Assert.False(legacy.Settings.BoundaryPainting.Enabled);
        Assert.All(
            BuiltInFlowPainterPresets.All.Where(preset => !ReferenceEquals(preset, legacy)),
            preset => Assert.True(preset.Settings.BoundaryPainting.Enabled));
    }

    [Theory]
    [InlineData("Soft contour")]
    [InlineData("Strong silhouette")]
    [InlineData("Loose background")]
    public void BuiltInCatalogProvidesBoundaryAwareArtisticPresets(string name)
    {
        FlowPainterPreset preset = Assert.Single(
            BuiltInFlowPainterPresets.All,
            candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));

        Assert.True(preset.Settings.BoundaryPainting.Enabled);
    }


    [Fact]
    public void BalancedPresetEnablesBackgroundSuppression()
    {
        FlowPainterPreset balanced = Assert.Single(
            BuiltInFlowPainterPresets.All,
            preset => string.Equals(preset.Name, "Balanced", StringComparison.Ordinal));

        Assert.True(balanced.Settings.BackgroundSuppression.Enabled);
    }

    [Fact]
    public void LooseBackgroundPresetUsesStrongerSuppressionThanBalanced()
    {
        FlowPainterPreset balanced = Assert.Single(
            BuiltInFlowPainterPresets.All,
            preset => string.Equals(preset.Name, "Balanced", StringComparison.Ordinal));
        FlowPainterPreset loose = Assert.Single(
            BuiltInFlowPainterPresets.All,
            preset => string.Equals(preset.Name, "Loose background", StringComparison.Ordinal));

        Assert.True(
            loose.Settings.BackgroundSuppression.OverallStrength
            > balanced.Settings.BackgroundSuppression.OverallStrength);
        Assert.True(
            loose.Settings.BackgroundSuppression.StrokeWidthMultiplier
            > balanced.Settings.BackgroundSuppression.StrokeWidthMultiplier);
    }

    [Fact]
    public void LegacyComparisonKeepsBackgroundSuppressionDisabled()
    {
        FlowPainterPreset legacy = Assert.Single(
            BuiltInFlowPainterPresets.All,
            preset => string.Equals(preset.Name, "Legacy comparison", StringComparison.Ordinal));

        Assert.False(legacy.Settings.BackgroundSuppression.Enabled);
    }

    [Fact]
    public void BuiltInCatalogProvidesDistinctNamedPresets()
    {
        Assert.True(BuiltInFlowPainterPresets.All.Count >= 4);
        Assert.Equal(
            BuiltInFlowPainterPresets.All.Count,
            BuiltInFlowPainterPresets.All.Select(preset => preset.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
