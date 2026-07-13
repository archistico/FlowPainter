using System.Globalization;
using System.Text.Json;
using FlowPainter.Application.FlowPainting.Legacy;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.FlowPainting.Legacy;

internal sealed class LegacyFlowFixture
{
    private LegacyFlowFixture(string version, RgbaImage source, LegacyDensityMap densityMap)
    {
        Version = version;
        Source = source;
        DensityMap = densityMap;
    }

    public string Version { get; }

    public RgbaImage Source { get; }

    public LegacyDensityMap DensityMap { get; }

    public static LegacyFlowFixture Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "legacy-flow-fixture.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;

        string version = root.GetProperty("version").GetString()
            ?? throw new InvalidDataException("The fixture version is missing.");
        int width = root.GetProperty("width").GetInt32();
        int height = root.GetProperty("height").GetInt32();
        ImageSize size = new(width, height);

        List<Rgba32> pixels = [];
        foreach (JsonElement element in root.GetProperty("pixelsRgba").EnumerateArray())
        {
            pixels.Add(ParseColor(element));
        }

        List<double> density = [];
        foreach (JsonElement element in root.GetProperty("density").EnumerateArray())
        {
            density.Add(element.GetDouble());
        }

        return new LegacyFlowFixture(
            version,
            new RgbaImage(size, pixels.ToArray()),
            new LegacyDensityMap(size, density.ToArray()));
    }

    private static Rgba32 ParseColor(JsonElement element)
    {
        string value = element.GetString()
            ?? throw new InvalidDataException("A fixture colour is missing.");
        uint packed = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return Rgba32.FromRgbaUInt32(packed);
    }
}
