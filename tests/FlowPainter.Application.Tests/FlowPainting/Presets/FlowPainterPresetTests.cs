using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;

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
    public void BuiltInCatalogProvidesDistinctNamedPresets()
    {
        Assert.True(BuiltInFlowPainterPresets.All.Count >= 4);
        Assert.Equal(
            BuiltInFlowPainterPresets.All.Count,
            BuiltInFlowPainterPresets.All.Select(preset => preset.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
