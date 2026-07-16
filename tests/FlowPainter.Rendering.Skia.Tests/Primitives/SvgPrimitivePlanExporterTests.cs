using System.Text;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Rendering.Skia.Primitives;

namespace FlowPainter.Rendering.Skia.Tests.Primitives;

public sealed class SvgPrimitivePlanExporterTests
{
    [Fact]
    public async Task ExportAsyncWritesAllSupportedSvgElements()
    {
        PrimitiveKind[] kinds = Enum.GetValues<PrimitiveKind>();
        GeometricPrimitive[] primitives = kinds
            .Select((kind, index) => new GeometricPrimitive(
                index,
                kind,
                new NormalizedPoint(0.2d + (0.12d * index), 0.5d),
                0.1d,
                0.08d,
                0.2d,
                new Rgba32(20, 40, 60, 200)))
            .ToArray();
        PrimitivePlan plan = new(
            new ImageSize(100, 80),
            1UL,
            Rgba32.Opaque(1, 2, 3),
            primitives,
            "test");
        using MemoryStream stream = new();

        await SvgPrimitivePlanExporter.ExportAsync(plan, new ImageSize(500, 400), stream);

        string svg = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("<polygon", svg, StringComparison.Ordinal);
        Assert.Contains("<rect", svg, StringComparison.Ordinal);
        Assert.Contains("<circle", svg, StringComparison.Ordinal);
        Assert.Contains("<ellipse", svg, StringComparison.Ordinal);
        Assert.EndsWith("</svg>\n", svg, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAsyncTruncatesExistingStream()
    {
        PrimitivePlan plan = new(
            new ImageSize(10, 10),
            1UL,
            Rgba32.Opaque(0, 0, 0),
            [],
            "test");
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(new string('x', 500)));

        await SvgPrimitivePlanExporter.ExportAsync(plan, new ImageSize(10, 10), stream);

        string svg = Encoding.UTF8.GetString(stream.ToArray());
        Assert.DoesNotContain("xxxxxxxx", svg, StringComparison.Ordinal);
        Assert.EndsWith("</svg>\n", svg, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAsyncHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        PrimitivePlan plan = new(
            new ImageSize(10, 10),
            1UL,
            Rgba32.Opaque(0, 0, 0),
            [],
            "test");
        using MemoryStream stream = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SvgPrimitivePlanExporter.ExportAsync(
            plan,
            new ImageSize(10, 10),
            stream,
            cancellation.Token));
    }
}
