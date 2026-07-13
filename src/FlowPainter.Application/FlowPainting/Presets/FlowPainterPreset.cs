using FlowPainter.Application.FlowPainting.Planning;

namespace FlowPainter.Application.FlowPainting.Presets;

public sealed class FlowPainterPreset
{
    public FlowPainterPreset(string name, FlowPainterSettings settings)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A preset name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(settings);
        Name = name.Trim();
        Settings = settings;
    }

    public string Name { get; }

    public FlowPainterSettings Settings { get; }

    public override string ToString()
    {
        return Name;
    }
}
