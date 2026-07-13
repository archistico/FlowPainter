using FlowPainter.Domain.Color;

namespace FlowPainter.Domain.Tests.Color;

public sealed class Rgba32Tests
{
    [Fact]
    public void PackedRoundTripPreservesChannels()
    {
        Rgba32 expected = new(0x12, 0x34, 0x56, 0x78);

        Rgba32 actual = Rgba32.FromRgbaUInt32(expected.ToRgbaUInt32());

        Assert.Equal(expected, actual);
        Assert.Equal(0x12345678u, actual.ToRgbaUInt32());
    }

    [Fact]
    public void OpaqueSetsMaximumAlpha()
    {
        Rgba32 color = Rgba32.Opaque(1, 2, 3);

        Assert.Equal(new Rgba32(1, 2, 3, byte.MaxValue), color);
    }
}
